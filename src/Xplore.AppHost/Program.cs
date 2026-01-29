var builder = DistributedApplication.CreateBuilder(args);

// --- Infrastructure Resources ---
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("xploredb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// --- Application Services ---
var api = builder.AddProject<Projects.Xplore_API>("xplore-api")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Xplore_Worker>("xplore-worker")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

builder.Build().Run();
