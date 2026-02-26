var builder = DistributedApplication.CreateBuilder(args);

// --- Infrastructure Resources ---
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("xplore-db-data")
    .WithPgAdmin()
    .AddDatabase("xploredb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var qdrant = builder.AddQdrant("vectordb");

// --- Application Services ---
var api = builder.AddProject<Projects.Xplore_API>("xplore-api")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(qdrant)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(qdrant)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Xplore_Worker>("xplore-worker")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(qdrant)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(qdrant);

builder.Build().Run();
