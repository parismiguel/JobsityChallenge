using AutoMapper;
using Chat.Web.Helpers;
using Chat.Web.Models;
using Chat.Web.Models.ViewModels;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chat.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        #region Properties
        /// <summary>
        /// List of online users
        /// </summary>
        public readonly static List<UserViewModel> _Connections = new List<UserViewModel>();

        /// <summary>
        /// List of available chat rooms
        /// </summary>
        private readonly static List<RoomViewModel> _Rooms = new List<RoomViewModel>();

        /// <summary>
        /// Mapping SignalR connections to application users.
        /// (We don't want to share connectionId)
        /// </summary>
        private readonly static Dictionary<string, string> _ConnectionsMap = new Dictionary<string, string>();
        #endregion

        public void Send(string roomName, string message)
        {
            if (message.StartsWith("/private"))
            {
                SendPrivate(message);
                return;
            }

            if (message.StartsWith("/stock=APPL"))
            {
                SendStock(message, false);
                return;
            }

            if (message.StartsWith("/day_range=APPL"))
            {
                SendStock(message, true);
                return;
            }

            SendToRoom(roomName, message);

        }

        private void SendStock(string message, bool dayrange)
        {
            Response stockResponse = Apis.GestStock();

            if (stockResponse.Status)
            {
                StockModel model = ConvertCsvToStockModel(stockResponse.Message);

                string output = $"APPL quote is ${model.Close} per share";

                if (dayrange)
                {
                    output = $"APPL ({model.Symbol}) Days Low quote is ${model.Low} and Days High quote is ${model.High}";
                }

                using (var db = new ApplicationDbContext())
                {
                    var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();

                    // Create and save message in database
                    Message msg = new Message()
                    {
                        Content = output,
                        Timestamp = DateTime.Now.Ticks.ToString(),
                        FromUser = user,
                        IsPrivate = true
                    };

                    db.Messages.Add(msg);
                    db.SaveChanges();

                    // Broadcast the message
                    var messageViewModel = Mapper.Map<Message, MessageViewModel>(msg);

                    // Send the message
                    Clients.Caller.newMessage(messageViewModel);
                }

            }

        }

        private StockModel ConvertCsvToStockModel(string message)
        {
            StockModel output = new StockModel();

            List<string> lines = message.Split(new[] { "\r\n" }, StringSplitOptions.None)
             .ToList();

            try
            {
                if (lines.Count > 1)
                {
                    List<string> line = lines[1].Split(",".ToCharArray()).ToList();

                    output.Symbol = line[0];
                    output.Date = line[1];
                    output.Time = line[2];
                    output.Symbol = line[3];
                    output.High = double.Parse(line[4]);
                    output.Low = double.Parse(line[5]);
                    output.Close = double.Parse(line[6]);
                    output.Volume = int.Parse(line[7]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return output;
        }

        public void SendPrivate(string message)
        {
            // message format: /private(receiverName) Lorem ipsum...
            string[] split = message.Split(')');
            string receiver = split[0].Split('(')[1];

            if (_ConnectionsMap.TryGetValue(receiver, out string userId))
            {
                using (var db = new ApplicationDbContext())
                {

                    // Who is the sender;
                    var sender = _Connections.Where(u => u.Username == IdentityName).First();

                    message = Regex.Replace(message, @"\/private\(.*?\)", string.Empty).Trim();

                    var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();

                    // Create and save message in database
                    Message msg = new Message()
                    {
                        Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                        Timestamp = DateTime.Now.Ticks.ToString(),
                        FromUser = user,
                        IsPrivate = true
                    };

                    db.Messages.Add(msg);
                    db.SaveChanges();

                    // Broadcast the message
                    var messageViewModel = Mapper.Map<Message, MessageViewModel>(msg);

                    //// Build the message
                    //MessageViewModel messageViewModel = new MessageViewModel()
                    //{
                    //    From = sender.DisplayName,
                    //    Avatar = sender.Avatar,
                    //    To = "",
                    //    Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                    //    Timestamp = DateTime.Now.ToLongTimeString(),
                    //    IsPrivate = true
                    //};

                    // Send the message
                    Clients.Client(userId).newMessage(messageViewModel);
                    Clients.Caller.newMessage(messageViewModel);
                }

            }
        }

        public void SendToRoom(string roomName, string message)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                    var room = db.Rooms.Where(r => r.Name == roomName).FirstOrDefault();

                    // Create and save message in database
                    Message msg = new Message()
                    {
                        Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                        Timestamp = DateTime.Now.Ticks.ToString(),
                        FromUser = user,
                        ToRoom = room
                    };

                    db.Messages.Add(msg);
                    db.SaveChanges();

                    // Broadcast the message
                    var messageViewModel = Mapper.Map<Message, MessageViewModel>(msg);

                    Clients.Group(roomName).newMessage(messageViewModel);
                }
            }
            catch (Exception)
            {
                Clients.Caller.onError("Mensaje no enviado!");
            }
        }

        public void Join(string roomName)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).FirstOrDefault();

                if (user.CurrentRoom != roomName)
                {
                    // Remove user from others list
                    if (!string.IsNullOrEmpty(user.CurrentRoom))
                        Clients.OthersInGroup(user.CurrentRoom).removeUser(user);

                    // Join to new chat room
                    Leave(user.CurrentRoom);
                    Groups.Add(Context.ConnectionId, roomName);
                    user.CurrentRoom = roomName;

                    // Tell others to update their list of users
                    Clients.OthersInGroup(roomName).addUser(user);
                }
            }
            catch (Exception ex)
            {
                Clients.Caller.onError("No se pudo conectar al grupo de chat!" + ex.Message);
            }
        }

        private void Leave(string roomName)
        {
            Groups.Remove(Context.ConnectionId, roomName);
        }

        public void CreateRoom(string roomName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    // Accept: Letters, numbers and one space between words.
                    Match match = Regex.Match(roomName, @"^\w+( \w+)*$");
                    if (!match.Success)
                    {
                        Clients.Caller.onError("Nombre inválido de grupo!\nEl nombre del grupo debe contener sólo letras y números.");
                    }
                    else if (roomName.Length < 5 || roomName.Length > 20)
                    {
                        Clients.Caller.onError("El nombre del grupo debe tener entre 5 a 20 caracteres!");
                    }
                    else if (db.Rooms.Any(r => r.Name == roomName))
                    {
                        Clients.Caller.onError("Ya existe otro grupo con este nombre");
                    }
                    else
                    {
                        // Create and save chat room in database
                        var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                        var room = new Room()
                        {
                            Name = roomName,
                            UserAccount = user
                        };

                        db.Rooms.Add(room);
                        db.SaveChanges();

                        if (room != null)
                        {
                            // Update room list
                            var roomViewModel = Mapper.Map<Room, RoomViewModel>(room);
                            _Rooms.Add(roomViewModel);
                            Clients.All.addChatRoom(roomViewModel);
                        }
                    }
                }//using
            }
            catch (Exception ex)
            {
                Clients.Caller.onError("No se pudo crear el grupo de chat: " + ex.Message);
            }
        }

        public void DeleteRoom(string roomName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    // Delete from database
                    var room = db.Rooms.Where(r => r.Name == roomName && r.UserAccount.UserName == IdentityName).FirstOrDefault();
                    db.Rooms.Remove(room);
                    db.SaveChanges();

                    // Delete from list
                    var roomViewModel = _Rooms.First<RoomViewModel>(r => r.Name == roomName);
                    _Rooms.Remove(roomViewModel);

                    // Move users back to Lobby
                    Clients.Group(roomName).onRoomDeleted(string.Format("El grupo {0} se ha eliminado.\nSe le ha trasladado a la sala de espera!", roomName));

                    // Tell all users to update their room list
                    Clients.All.removeChatRoom(roomViewModel);
                }
            }
            catch (Exception)
            {
                Clients.Caller.onError("No se puede eliminar este grupo.");
            }
        }

        public IEnumerable<MessageViewModel> GetMessageHistory(string roomName)
        {
            using (var db = new ApplicationDbContext())
            {
                var messageHistory = db.Messages.Where(m => m.ToRoom.Name == roomName)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(50)
                    .AsEnumerable()
                    .Reverse()
                    .ToList();

                return Mapper.Map<IEnumerable<Message>, IEnumerable<MessageViewModel>>(messageHistory);
            }
        }

        public IEnumerable<RoomViewModel> GetRooms()
        {
            using (var db = new ApplicationDbContext())
            {
                // First run?
                if (_Rooms.Count == 0)
                {
                    foreach (var room in db.Rooms)
                    {
                        var roomViewModel = Mapper.Map<Room, RoomViewModel>(room);
                        _Rooms.Add(roomViewModel);
                    }
                }
            }

            return _Rooms.ToList();
        }

        public IEnumerable<UserViewModel> GetUsers(string roomName)
        {
            return _Connections.Where(u => u.CurrentRoom == roomName).ToList();
        }

        #region OnConnected/OnDisconnected
        public override Task OnConnected()
        {
            using (var db = new ApplicationDbContext())
            {
                try
                {
                    var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();

                    var userViewModel = Mapper.Map<ApplicationUser, UserViewModel>(user);
                    userViewModel.Device = GetDevice();
                    userViewModel.CurrentRoom = "";

                    _Connections.Add(userViewModel);
                    _ConnectionsMap.Add(IdentityName, Context.ConnectionId);

                    Clients.Caller.getProfileInfo(user.DisplayName, user.Avatar);
                }
                catch (Exception ex)
                {
                    Clients.Caller.onError("OnConnected:" + ex.Message);
                }
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).First();
                _Connections.Remove(user);

                // Tell other users to remove you from their list
                Clients.OthersInGroup(user.CurrentRoom).removeUser(user);

                // Remove mapping
                _ConnectionsMap.Remove(user.Username);
            }
            catch (Exception ex)
            {
                Clients.Caller.onError("OnDisconnected: " + ex.Message);
            }

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            var user = _Connections.Where(u => u.Username == IdentityName).First();
            Clients.Caller.getProfileInfo(user.DisplayName, user.Avatar);

            return base.OnReconnected();
        }
        #endregion

        private string IdentityName
        {
            get { return Context.User.Identity.Name; }
        }

        private string GetDevice()
        {
            string device = Context.Headers.Get("Device");

            if (device != null && (device.Equals("Desktop") || device.Equals("Mobile")))
                return device;

            return "Web";
        }
    }
}