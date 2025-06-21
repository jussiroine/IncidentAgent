using System.ComponentModel;
using Microsoft.Extensions.AI;

#region Functions
[Description("Provide security recommendations based on the alert")]
string SecurityRecommendation([Description("Security alert response recommendation")] string alert) => "Use MFA, reset user password, check for any suspicious activity, and monitor the user account for any further alerts.";
#endregion

var chatOptions = new ChatOptions
{
    Tools = new List<AITool> { AIFunctionFactory.Create(SecurityRecommendation) },
    ToolMode = AutoChatToolMode.Auto,
    Temperature = 0.5f
};

var endpoint = "http://localhost:11434/";
var modelId = "llama3.2:3b";

Console.ForegroundColor = ConsoleColor.White;
Console.Write("Initiating..");

IChatClient client = new OllamaChatClient(endpoint, modelId: modelId)
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

Console.ForegroundColor = ConsoleColor.Green;
Console.Write(" [OK]\n");

var messages = new List<ChatMessage>();
var sysMessage = new ChatMessage(ChatRole.System,"""
    You are a security advisor, focusing on security incidents and alerts. Provide recommendations based on the alert. Always produce a list of action. In the end, add a highlighted comment of ONE recommendation that is the best. 
    """);

messages.Add(sysMessage);

string filePath = @"..\incident.json";
string fileContent = await File.ReadAllTextAsync(filePath);

var response = await client.CompleteAsync(messages + "I have the following security alert: " + fileContent + " Give me security recommendation based on the alert. Do not use markdown for formatting. Highlight ONE critical action in the beginning.", chatOptions);
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine(response.Message);



