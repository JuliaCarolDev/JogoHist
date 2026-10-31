using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BrasilQuiz.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Period { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectAnswerIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    public class Player
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; } = 0;
        public bool IsHost { get; set; } = false;
    }

    public class QuizRoom
    {
        public string RoomCode { get; set; } = string.Empty;
        public List<Player> Players { get; set; } = new();
        public int CurrentQuestionIndex { get; set; } = 0;
        public bool IsGameStarted { get; set; } = false;
    }
}

namespace BrasilQuiz.Hubs
{
    using BrasilQuiz.Models;

    public class QuizHub : Hub
    {
        private static readonly ConcurrentDictionary<string, QuizRoom> Rooms = new();

        public async Task CreateRoom(string playerName)
        {
            string roomCode = "BR-" + Random.Shared.Next(100, 999).ToString();
            var room = new QuizRoom { RoomCode = roomCode };
            var player = new Player { ConnectionId = Context.ConnectionId, Name = playerName, IsHost = true };
            
            room.Players.Add(player);
            Rooms[roomCode] = room;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("RoomCreated", roomCode, room.Players);
        }

        public async Task JoinRoom(string roomCode, string playerName)
        {
            string upperCode = roomCode.ToUpper();
            if (Rooms.TryGetValue(upperCode, out var room))
            {
                if (room.Players.Count >= 4)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", "Sala cheia! Máximo de 4 jogadores.");
                    return;
                }

                var player = new Player { ConnectionId = Context.ConnectionId, Name = playerName, IsHost = false };
                room.Players.Add(player);

                await Groups.AddToGroupAsync(Context.ConnectionId, upperCode);
                await Clients.Group(upperCode).SendAsync("UpdatePlayerList", room.Players);
                await Clients.Caller.SendAsync("JoinedRoomSuccess", upperCode, room.Players);
            }
            else
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Código de sala inválido ou inexistente.");
            }
        }

        public async Task StartGame(string roomCode)
        {
            if (Rooms.TryGetValue(roomCode.ToUpper(), out var room))
            {
                room.IsGameStarted = true;
                await Clients.Group(roomCode.ToUpper()).SendAsync("GameStarted");
            }
        }

        public async Task SubmitAnswer(string roomCode, int selectedIndex, int timeRemaining)
        {
            if (Rooms.TryGetValue(roomCode.ToUpper(), out var room))
            {
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    int points = 100 + (timeRemaining * 10);
                    player.Score += points;
                    await Clients.Group(roomCode.ToUpper()).SendAsync("UpdateScores", room.Players.OrderByDescending(p => p.Score).ToList());
                }
            }
        }
    }
}