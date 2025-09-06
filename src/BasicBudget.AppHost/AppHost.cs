
var builder = DistributedApplication.CreateBuilder(args);

// Add Postgres container resource
var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

// Add the GraphQL API project and inject the connection string
var app = builder.AddProject<Projects.BasicBudget_GraphQL>("graphql-api")
       .WithReference(postgresdb);
app.WaitFor(postgresdb);


builder.Build().Run();