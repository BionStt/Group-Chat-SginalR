using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;


namespace SignalRChat
{
    /*
     * SignalRChatHub is my SignalR class for one-to-one chat.
     * [HubName("signalRChatHub")]” represents the custom hub name
     * **/
    [HubName("signalRChatHub")]
    public class SignalRChatHub : Hub
    {
        #region---Data Members---
        static List<UserDetail> ConnectedUsers = new List<UserDetail>();
        static List<MessageDetail> CurrentMessage = new List<MessageDetail>();
        #endregion
      
        SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["CHCON"].ConnectionString);

        public void BroadCastMessage(String msgFrom, String msg)
        {                      
            var id = Context.ConnectionId;
            Clients.All.receiveMessage(msgFrom, msg, "");            
            /*string[] Exceptional = new string[1];
            Exceptional[0] = id;       
            Clients.AllExcept(Exceptional).receiveMessage(msgFrom, msg);*/           
        }

        /* 
         * hub from client side, I will call the custom method hubconnect from client to get store 
         * connected user details to MS SQL database.
         * **/
        [HubMethodName("hubconnect")]
        public void Get_Connect(String username,String userid,String connectionid)
        {
            string count = "";
            string msg = "";
            string list = "";
            try
            {
                count = GetCount().ToString();
                
                /* 
                 * Method “updaterec” is used to store user details to database
                 * **/
                msg = updaterec(username, userid, connectionid);
                /*
                * “GetUsers” will return the user names and their connection ids other than current user.
                * **/  
                list = GetUsers(username);
            }
            catch (Exception d)
            {
                msg = "DB Error "+d.Message;
            }
            var id = Context.ConnectionId;
            
            string[] Exceptional = new string[1];
            Exceptional[0] = id;

            /* * 
             * “Clients.Caller.receiveMessage” method is used where “receiveMessage” 
             * is a client side method to be called from signalR hub server. 
             * **/
            Clients.Caller.receiveMessage("RU", msg, list);

            /*
             * To caller, the list of all online users with connection ids was send 
             * while to other online users, the information about newly connected user is send. 
             * “Exceptional” array is used to make the collection of users to whom, message is not 
             * to send. Here, in my case, only the caller is one user, to whom message was not to send.
             * **/ 
            Clients.AllExcept(Exceptional).receiveMessage("NewConnection", username+" "+id,count);            
        }

        /* 
         * For private chat message broadcast, I will write the following custom method.
         **/
        //[HubMethodName("privatemessage")]
        public void Send_PrivateMessage(String msgFrom, String msg, String touserid)
        {
            var id = Context.ConnectionId;

            /* * 
            * “Clients.Caller.receiveMessage” method is used where “receiveMessage” 
            * is a client side method to be called from signalR hub server. 
            * **/
            //“Clients.Caller.receiveMessage” is called to broadcast the message to sender while 
            Clients.Caller.receiveMessage(msgFrom, msg,touserid);
            // “Clients.Client(touserid).receiveMessage” is called to broadcast the message to single receiver. Here “touserid” is the connection id of receiver.
            Clients.Client(touserid).receiveMessage(msgFrom, msg,id);
        }

        /*
         * override the method “OnConnected()” to get connection id of connected client and inform 
         * connected client with total number of connected users.
         **/
        public override System.Threading.Tasks.Task OnConnected()
        {
            //string username = Context.QueryString["username"].ToString();
            string clientId = Context.ConnectionId;
            string data =clientId;
            string count = "";
            try
            {
                count= GetCount().ToString();
            }
            catch (Exception d)
            {
                count = d.Message;
            }

            /* * 
            * “Clients.Caller.receiveMessage” method is used where “receiveMessage” 
            * is a client side method to be called from signalR hub server. 
            * **/
            Clients.Caller.receiveMessage("ChatHub", data, count);
            return base.OnConnected();
        }

        public override System.Threading.Tasks.Task OnReconnected()
        {         
            return base.OnReconnected();
        }

        /* 
         * The “OnDisconnected” method is override to remove the disconnected client from database. 
         * Also the information of disconnected client is broadcasted to other users.
         * **/
        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            string count = "";
            string msg = "";
           
            string clientId = Context.ConnectionId;
            DeleteRecord(clientId);

            try
            {
                count = GetCount().ToString();
            }
            catch (Exception d)
            {
                msg = "DB Error " + d.Message;
            }
            string[] Exceptional = new string[1];
            Exceptional[0] = clientId;
            /*
             * To caller, the list of all online users with connection ids was send 
             * while to other online users, the information about newly connected user is send. 
             * “Exceptional” array is used to make the collection of users to whom, message is not 
             * to send. Here, in my case, only the caller is one user, to whom message was not to send.
             * **/
            Clients.AllExcept(Exceptional).receiveMessage("NewConnection", clientId+" leave", count);

            return base.OnDisconnected(stopCalled);
        }

        /* 
         * Method “updaterec” is used to store user details to database
         * **/
        public string updaterec(string username,string userid, string connectionid)
        {
            try
            {
                SqlCommand save = new SqlCommand("insert into [ChatUsers] values('" + username + "','" + userid + "','" + connectionid + "')", sqlcon);
                sqlcon.Open();
                int rs = save.ExecuteNonQuery();
                sqlcon.Close();
                return "saved";
            }
            catch (Exception d)
            {
                sqlcon.Close();
                return d.Message;
            }            
        }


        /*
         * GetCount() is a method used to get total number of users connected to hub. 
         * I will store the connected users to MS SQL Database on successful connection. 
         * To inform the connected user,  
         **/
        public int GetCount()
        {
            int count = 0;

            try
            {
                SqlCommand getCount = new SqlCommand("select COUNT([UserName]) as TotalCount from [ChatUsers]", sqlcon);
                sqlcon.Open();
                count = int.Parse(getCount.ExecuteScalar().ToString());
            }
            catch (Exception)
            {
            }
            sqlcon.Close();
            return count;
        }

        public bool DeleteRecord(string connectionid)
        {
            bool result = false;

            try
            {
                SqlCommand deleterec = new SqlCommand("delete from [ChatUsers] where ([ConnectionID]='" + connectionid + "')", sqlcon);
                sqlcon.Open();
                deleterec.ExecuteNonQuery();
                result = true;
            }
            catch (Exception)
            {   
            }
            sqlcon.Close();
            return result;
        }
        /*
         * “GetUsers” will return the user names and their connection ids other than current user.
         * **/
        public string GetUsers(string username)
        {
            string list = "";

            try
            {
                int count = GetCount();
                SqlCommand listrec = new SqlCommand("select [UserName],[ConnectionID] from [ChatUSers] where ([UserName]<>'" + username + "')", sqlcon);
                sqlcon.Open();
                SqlDataReader reader = listrec.ExecuteReader();
                reader.Read();

                for (int i = 0; i < (count-1); i++)
                {
                    list += reader.GetValue(0).ToString() + " ( " + reader.GetValue(1).ToString() + " )#";
                    reader.Read();
                }
            }
            catch (Exception)
            {
            }
            sqlcon.Close();
            return list;
        }

        public void Create_Group(string GroupName)
        {
            
        }

        private string GetClientId()
        {  
            string clientId = "";
            if (Context.QueryString["clientId"] != null)
            {
                // clientId passed from application 
                clientId = this.Context.QueryString["clientId"];
            }

            if (string.IsNullOrEmpty(clientId.Trim()))
            {
                clientId = Context.ConnectionId;
            }

            return clientId;
        }
    }
}