﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace EW.NetWork
{
    public class HandshakeRequest
    {
        public string Mod;
        public string Version;
        public string Map;

        public string Serialize()
        {
            var data = new List<MiniYamlNode>();
            data.Add(new MiniYamlNode("Handshake", FieldSaver.Save(this)));
            return data.WriteToString();
        }


        public static HandshakeRequest Deserialize(string data)
        {
            var handshake = new HandshakeRequest();
            FieldLoader.Load(handshake, MiniYaml.FromString(data).First().Value);
            return handshake;
        }
    }

    public class HandshakeResponse
    {
        public string Mod;
        public string Version;
        public string Password;

        [FieldLoader.Ignore]
        public Session.Client Client;

        public static HandshakeResponse Deserialize(string data)
        {
            var handshake = new HandshakeResponse();
            handshake.Client = new Session.Client();

            var ys = MiniYaml.FromString(data);
            foreach(var y in ys)
            {
                switch (y.Key)
                {
                    case "Handshake":
                        FieldLoader.Load(handshake, y.Value);
                        break;
                    case "Client":
                        FieldLoader.Load(handshake.Client, y.Value);
                        break;
                }
            }
            return handshake;
        }

        public string Serialize()
        {
            var data = new List<MiniYamlNode>();
            data.Add(new MiniYamlNode("Handshake", null, new string[] { "Mod", "Version", "Password" }.Select(p => FieldSaver.SaveField(this, p)).ToList()));

            data.Add(new MiniYamlNode("Client", FieldSaver.Save(Client)));

            return data.WriteToString();
        }
    }



}