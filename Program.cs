using BrasilQuiz.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Adicionar suporte ao SignalR
builder.Services.AddSignalR();

// Adicionar CORS se o frontend estiver rodando em outra porta/domínio
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .SetIsOriginAllowed(_ => true)
               .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("CorsPolicy");

// Mapear o endpoint do SignalR (o frontend se conectará em "/quizHub")
app.MapHub<QuizHub>("/quizHub");

app.Run();