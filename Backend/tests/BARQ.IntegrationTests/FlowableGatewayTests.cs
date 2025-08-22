using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using BARQ.Application.Services.Workflow;
using BARQ.Core.Services;

namespace BARQ.IntegrationTests;

public class FlowableGatewayTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly IFlowableGateway _flowableGateway;

    public FlowableGatewayTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _flowableGateway = _factory.Services.GetRequiredService<IFlowableGateway>();
    }

    [Fact]
    public async Task DeployAsync_ShouldDeployBpmnProcess()
    {
        var bpmnContent = CreateSampleBpmnProcess();
        using var bpmnStream = new MemoryStream(bpmnContent);

        await _flowableGateway.DeployAsync(bpmnStream);
    }

    [Fact]
    public async Task StartAsync_ShouldStartProcessInstance()
    {
        var bpmnContent = CreateSampleBpmnProcess();
        using var bpmnStream = new MemoryStream(bpmnContent);
        
        await _flowableGateway.DeployAsync(bpmnStream);

        var variables = new { taskId = "test-task-123", priority = "high" };

        var processInstanceId = await _flowableGateway.StartAsync("barq-task-workflow", variables);

        Assert.NotNull(processInstanceId);
        Assert.NotEmpty(processInstanceId);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldReturnTasks()
    {
        var bpmnContent = CreateSampleBpmnProcess();
        using var bpmnStream = new MemoryStream(bpmnContent);
        
        await _flowableGateway.DeployAsync(bpmnStream);
        var processInstanceId = await _flowableGateway.StartAsync("barq-task-workflow", new { });

        var tasks = await _flowableGateway.GetTasksAsync("", "reviewers");

        Assert.NotNull(tasks);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnProcessHistory()
    {
        var bpmnContent = CreateSampleBpmnProcess();
        using var bpmnStream = new MemoryStream(bpmnContent);
        
        await _flowableGateway.DeployAsync(bpmnStream);
        var processInstanceId = await _flowableGateway.StartAsync("barq-task-workflow", new { });

        var history = await _flowableGateway.GetHistoryAsync(processInstanceId);

        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    private static byte[] CreateSampleBpmnProcess()
    {
        var bpmnXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<definitions xmlns=""http://www.omg.org/spec/BPMN/20100524/MODEL""
             xmlns:flowable=""http://flowable.org/bpmn""
             targetNamespace=""http://barq.example.com/workflows"">
  
  <process id=""barq-task-workflow"" name=""BARQ Task Workflow"" isExecutable=""true"">
    
    <startEvent id=""start"" name=""Task Created""/>
    
    <userTask id=""review-task"" name=""Review Task"" 
              flowable:candidateGroups=""reviewers"">
      <documentation>Review and approve the task</documentation>
    </userTask>
    
    <exclusiveGateway id=""decision"" name=""Approved?""/>
    
    <userTask id=""rework-task"" name=""Rework Required"">
      <documentation>Address review comments</documentation>
    </userTask>
    
    <serviceTask id=""complete-task"" name=""Complete Task""
                 flowable:expression=""#{true}""/>
    
    <endEvent id=""end"" name=""Task Completed""/>
    
    <!-- Sequence Flows -->
    <sequenceFlow sourceRef=""start"" targetRef=""review-task""/>
    <sequenceFlow sourceRef=""review-task"" targetRef=""decision""/>
    <sequenceFlow sourceRef=""decision"" targetRef=""rework-task"">
      <conditionExpression>${!approved}</conditionExpression>
    </sequenceFlow>
    <sequenceFlow sourceRef=""decision"" targetRef=""complete-task"">
      <conditionExpression>${approved}</conditionExpression>
    </sequenceFlow>
    <sequenceFlow sourceRef=""rework-task"" targetRef=""review-task""/>
    <sequenceFlow sourceRef=""complete-task"" targetRef=""end""/>
    
  </process>
  
</definitions>";

        return Encoding.UTF8.GetBytes(bpmnXml);
    }
}

public class MockTenantProvider : ITenantProvider
{
    private Guid _tenantId = Guid.Parse("12345678-1234-1234-1234-123456789012");

    public Guid GetTenantId() => _tenantId;

    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;

    public string GetTenantName() => "Test Tenant";

    public void ClearTenantContext() => _tenantId = Guid.Empty;

    public Guid GetCurrentUserId() => Guid.Parse("12345678-1234-1234-1234-123456789013");
}
