using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Resume.Controllers;
using Resume.DTO.JobDTO;
using Resume.Helpers;
using Resume.Models;
using Resume.Repository.IRepository;
using Xunit;
using Resume.Mapper;

public class JobControllerTests
{
    // Dependency injection
    private readonly Mock<IJobRepository> _jobRepoMock; // We need to mock it because its a repository
    private readonly Mock<IResumeRepository> _resumeRepoMock; // We also need to mock the resume repo since its injected into jobcontroller too
    private readonly JobController _jobCont;
    public JobControllerTests()
    {
        // Mock the JobRepository
        _jobRepoMock = new Mock<IJobRepository>();
        _resumeRepoMock = new Mock<IResumeRepository>();

        _jobCont = new JobController(_jobRepoMock.Object, _resumeRepoMock.Object);
    }

    // Test GetAll() returns ok with a list of jobs
    [Fact]
    public async Task GetAll_ReturnsWithJobs()
    {
        // Arrange: Prepare test data
        var query = new QueryObject { PageNumber = 1, PageSize = 10 }; // Simulated query object
        var jobs = new List<Job>
        {
            new Job { Id = 1, JobTitle = "Senior Tech Engineer", JobDescription = new List<string> {"Testing JobList 1", "Testing JobList 2"}, IsCurrentJob = true },
            new Job { Id = 2, JobTitle = "Project Manager", JobDescription = new List<string> {"Testing JobList 1", "Testing JobList 2"}, IsCurrentJob = false }
        };

        // Mock GetAllAsync to return the list of jobs
        _jobRepoMock.Setup(repo => repo.GetAllAsync(query)).ReturnsAsync(jobs);

        // Act: Call the GetAll() method of the controller
        var result = await _jobCont.GetAll(query);

        // Assert: Verify that the response is ok and contains expected jobs
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<MyJobDTOs>>().Subject; 
        returnJobs.Should().HaveCount(2); // Check that two jobs should be returned
    }

    // Test to ensure GetById works
    [Fact]
    public async Task GetByID_ReturnsWithJobs()
    {
        // Arrange: Prepare mock data
        var jobId = 1;
        var job = new Job { Id = jobId, JobTitle = "Senior Tech Engineer", JobDescription = new List<string> {"Testing JobList 1", "Testing JobList 2"}, IsCurrentJob = true};

        // Mock: the GetByIdAsync to return the job
        _jobRepoMock.Setup(repo => repo.GetByIdAsync(jobId)).ReturnsAsync(job);

        // Act: Call GetById method from controller
        var result = await _jobCont.GetById(jobId);

        // Asset: Verify the response is ok and job matches the expected jobId
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject; // Check the result
        var returnJob = okResult.Value.Should().BeAssignableTo<MyJobDTOs>().Subject; // Check that the result matches
        returnJob.Id.Should().Be(jobId); // Checks if the specified id matches the expected id
    }

    // Test to ensure that the Create works if resume exists
    [Fact]
    public async Task Create_ReturnWithJobs()
    {
        // Arrange: Prepare the data
        var resumeId = 1; 
        var jobDTO = new CreateJobDTO 
        {
            JobTitle = "Senior Tech Engineer", 
            JobDescription = new List<string> {"Testing JobList 1", "Testing JobList 2"}, 
            IsCurrentJob = true
        };

        var job = jobDTO.CreateJOB(resumeId); // Map the DTO to the job model and assign it to the resume by resumeId

        // Mock: Get the resume and then create job asynchronously 
        _resumeRepoMock.Setup(repo => repo.ResumeExist(resumeId)).ReturnsAsync(true); // States that the resume exists
        _jobRepoMock.Setup(repo => repo.CreateJobAsync(It.IsAny<Job>())).ReturnsAsync(job);


        // Act: Call the Create() method of the controller
        var result = await _jobCont.Create(resumeId, jobDTO);

        // Assert
        var createResult = result.Should().BeOfType<CreatedAtActionResult>().Subject; // Makes sure its successfully created
        var returnJob = createResult.Value.Should().BeAssignableTo<MyJobDTOs>().Subject;
        returnJob.JobTitle.Should().Be("Senior Tech Engineer"); // Fetch the job title and make sure it matches
    }

    // Test to make sure to return badrequest for Create() if the resume does not exist
    [Fact]
    public async Task Create_ReturnFalseForNoResume()
    {
        // Arrange
        var resumeId = 1;
        var jobDTO = new CreateJobDTO 
        {
            JobTitle = "Senior Tech Engineer", 
            JobDescription = new List<string> {"Testing JobList 1", "Testing JobList 2"}, 
            IsCurrentJob = true
        };

        // Mock
        // Since the resume doesn't exist, return false
        _resumeRepoMock.Setup(repo => repo.ResumeExist(resumeId)).ReturnsAsync(false);

        // Act
        var result = await _jobCont.Create(resumeId, jobDTO);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Resume does not exist");
    }

    // Check the Delete() works as long as the job exist
    [Fact]
    public async Task Delete_ReturnsTrueIfJobExist()
    {
        // Arrange test data
        var jobId = 1;
        var job = new Job
        {
            Id = jobId, 
            JobTitle = "Senior Tech Engineer", 
            JobDescription = new List<string> {"Testing JobList 1", "Testing JobList 2"}, 
            IsCurrentJob = true
        };

        // Mock: Use DeleteJobAsync to return the job that is deleted
        _jobRepoMock.Setup(repo => repo.DeleteJobAsync(jobId)).ReturnsAsync(job);

        // Act: Call the Delete() method of the controller
        var result = await _jobCont.Delete(jobId);

        // Assert: Verify that the job is deleted
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnJob = okResult.Value.Should().BeAssignableTo<Job>().Subject;
        returnJob.Id.Should().Be(jobId);
    }

    // Ensure that the Delete() returns NotFound if the job doesn't exist
    [Fact]
    public async Task Delete_ReturnFalse_JobDoesNotExist()
    {
        // Arrange
        var jobId = 1;

        // Mock
        _jobRepoMock.Setup(repo => repo.DeleteJobAsync(jobId)).ReturnsAsync((Job)null);

        // Act
        var result = await _jobCont.Delete(jobId);

        // Assert: verify thta the reponse is a notfound result
        result.Should().BeOfType<NotFoundResult>();
    }
}
