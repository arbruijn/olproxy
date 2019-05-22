using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace olproxy
{
    class PrivateMatchData
    {
        private static readonly string[] StockLevels = new[] { "Wraith", "Vault",  "Terminal", "Blizzard", "Backfire", "Centrifuge",
            "Labyrinth", "Hive", "Syrinx", "Foundry", "Roundabout" };
        private static readonly string[] MatchModes = new[] { "Anarchy", "Team Anarchy", "Monsterball" };
        private static readonly int[] TimeLimits = new[] { 0, 3, 5, 7, 10, 15, 20 };

        private class BitReader
        {
            private byte[] buf;
            private int p, o;
            public BitReader(byte[] buf)
            {
                this.buf = buf;
            }
            public int Read(int n)
            {
                int mask = (1 << n) - 1;
                int b1 = buf[p] >> o;
                if (n + o <= 8)
                {
                    if ((o += n) == 8)
                    {
                        o = 0;
                        p++;
                    }
                    return b1 & mask;
                }
                int rd = 8 - o;
                p++;
                o = n - rd;
                return (b1 | (buf[p] << rd)) & mask;
            }
            public string ReadString()
            {
                var n = Read(8);
                return Encoding.UTF8.GetString(Enumerable.Range(0, n).Select(_ => (byte)Read(8)).ToArray());
            }
            public int ReadInt()
            {
                return BitConverter.ToInt32(Enumerable.Range(0, 4).Select(_ => (byte)Read(8)).ToArray(), 0);
            }
        }

        public string Creator;
        public int Mode;
        public int MinPlayers;
        public int MaxPlayers;
        public int StockLevelNum;
        public string CustomLevel;
        public int TimeLimit;
        public int ScoreLimit;
        public int RespawnTime;
        public int RespawnShield;
        public int FriendlyFire;
        public int ShowEnemyNames;
        public int TurnSpeedLimit;
        public int PowerupFreq;
        public int PowerupInit;
        public int PowerupSuperFreq;
        public int PowerupAllowed;
        public int CustomLoadout;
        public int LoadoutWeapon1;
        public int LoadoutWeapon2;
        public int LoadoutMissile1;
        public int LoadoutMissile2;
        public int CustomModifier1;
        public int CustomModifier2;
        public bool JIPEnabled;

        public string LevelName
        {
            get 
            {
                if (StockLevelNum >= 0)
                    return StockLevelNum < StockLevels.Length ? StockLevels[StockLevelNum] : "Level " + StockLevelNum;
                return CustomLevel.Substring(0, CustomLevel.LastIndexOf('.'));
            }
        }

        public string GameMode
        {
            get
            {
                return Mode < MatchModes.Length ? MatchModes[Mode] : "Mode " + Mode;
            }
        }

        public PrivateMatchData(byte[] buf)
        {
            var br = new BitReader(buf);
            Creator = br.ReadString().ToUpper();
            var i = Creator.IndexOf('\0'); // olmod extension
            if (i >= 0)
            {
                JIPEnabled = i + 1 < Creator.Length && (Creator[i + 1] & 8) != 0;
                Creator = Creator.Substring(0, i);
            }
            else
                JIPEnabled = false;
            Mode = br.Read(3);
            MinPlayers = br.Read(4);
            MaxPlayers = br.Read(4);
            if (MaxPlayers == 0) // olmod extension
                MaxPlayers = 16;
            if (br.Read(1) == 0)
            {
                CustomLevel = br.ReadString();
                StockLevelNum = -1;
            }
            else
                StockLevelNum = br.Read(6);
            TimeLimit = br.Read(4);
            ScoreLimit = br.ReadInt();
            RespawnTime = br.Read(8);
            RespawnShield = br.Read(8);
            FriendlyFire = br.Read(1);
            ShowEnemyNames = br.Read(3);
            TurnSpeedLimit = br.Read(3);
            PowerupFreq = br.Read(3);
            PowerupInit = br.Read(8);
            PowerupSuperFreq = br.Read(3);
            PowerupAllowed = br.ReadInt();
            CustomLoadout = br.Read(2);
            LoadoutWeapon1 = br.Read(4);
            LoadoutWeapon2 = br.Read(4);
            LoadoutMissile1 = br.Read(4);
            LoadoutMissile2 = br.Read(4);
            CustomModifier1 = br.Read(8);
            CustomModifier2 = br.Read(8);
        }

/*
        static void MainTest()
        {
            //System.Convert.FromBase64String
            string s = @"{""max"":2,""name"":""MMRequest"",""uid"":""e0bd6714-4c4a-4c29-8151-5c7b01cd4eb3"",""attr"":{""mm_ticketType"":{""S"":""request""},""mm_name"":{""S"":""PRIVATE-Overload-PROD""},""mm_ticket"":{""S"":""1523454f-c7e1-4fa5-a101-d7c81c84373a""},""mm_version"":{""I"":11},""mm_players"":{""S"":""[{\""PlayerId\"":\""67e3e780-b368-11e8-a0d7-0d8a3d4259a5\"",\""Team\"":\""players\"",\""PlayerAttributes\"":{\""private_match_data\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""BGFybmWQUCFUJeX0VEXV0uTSBKVTBEN0E0QUJERkAAAAAwOgCKgBAAAAQEACAg==\""},\""password\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""vps1.2ar.nl\""]},\""private_initiator\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":1},\""max_num_players\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":1},\""devId\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""PROD\""},\""version\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":11},\""playlists\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""private\""]},\""skill\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":500},\""platform_self\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""pc\""]},\""platform_other\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""pc\"",\""ps4\"",\""xbox\""]},\""uid\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""167647c3-df72-41fd-ae63-2c00b5530966\""}}}]""},""mm_createTime"":{""S"":""48D6990197CCD472""}}}";
            // (@"{""max"":2,""name"":""MMRequest"",""uid"":""892d5795-31cd-4bf4-96e3-a3bc407eef91"",""attr"":{""mm_ticketType"":{""S"":""request""},""mm_name"":{""S"":""PRIVATE-Overload-PROD""},""mm_ticket"":{""S"":""5b83d89d-eab3-4744-9acd-132c71719e72""},""mm_version"":{""I"":11},""mm_players"":{""S"":""[{\""PlayerId\"":\""27a3b506-b368-11e8-a0d7-0d8a3d4259a5\"",\""Team\"":\""players\"",\""PlayerAttributes\"":{\""private_match_data\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""BGFybmWQGBAFAADAwAAoAmoAAAAAEJCAAA==\""},\""password\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""loc\""]},\""private_initiator\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":1},\""max_num_players\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":1},\""devId\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""PROD\""},\""version\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":11},\""playlists\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""private\""]},\""skill\"":{\""attributeType\"":\""DOUBLE\"",\""valueAttribute\"":500},\""platform_self\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""pc\""]},\""platform_other\"":{\""attributeType\"":\""STRING_LIST\"",\""valueAttribute\"":[\""pc\"",\""ps4\"",\""xbox\""]},\""uid\"":{\""attributeType\"":\""STRING\"",\""valueAttribute\"":\""99ea9514-186e-423f-875b-78a1452b59e6\""}}}]""},""mm_createTime"":{""S"":""48D630E5F3E43ACA""}}}"
            var obj1 = MiniJson.Parse(s);
            var obj = MiniJson.Parse((((obj1 as MJDict)["attr"] as MJDict)["mm_players"] as MJDict)["S"] as string);
            foreach (var pl in obj as List<object>)
                if (((pl as MJDict)["PlayerAttributes"] as MJDict).TryGetValue("private_match_data", out object data))
                {
                    var br = new BitReader(System.Convert.FromBase64String((data as MJDict)["valueAttribute"] as string));
                    var vals = new MJDict();
                    vals.Add("name", br.ReadString());
                    vals.Add("mode", br.Read(3));
                    vals.Add("min_players", br.Read(4));
                    vals.Add("max_players", br.Read(4));
                    if (br.Read(1) == 0)
                        vals.Add("custom_hash", br.ReadString());
                    else
                        vals.Add("level_num", br.Read(6));
                    vals.Add("time_limit", br.Read(4));
                    vals.Add("score_limit", br.ReadInt());
                    vals.Add("respawn_time", br.Read(8));
                    vals.Add("respawn_invul", br.Read(8));
                    vals.Add("friendly_fire", br.Read(1));
                    vals.Add("enemy_names", br.Read(3));
                    vals.Add("turn_speed_limit", br.Read(3));
                    vals.Add("powerup_spawn", br.Read(3));
                    vals.Add("powerup_start", br.Read(8));
                    vals.Add("super_powerup_spawn", br.Read(3));
                    vals.Add("powerup_filter", br.ReadInt());
                    vals.Add("force_loadout", br.Read(2));
                    vals.Add("force_w1", br.Read(4));
                    vals.Add("force_w2", br.Read(4));
                    vals.Add("force_m1", br.Read(4));
                    vals.Add("force_m2", br.Read(4));
                    vals.Add("force_mod1", br.Read(8));
                    vals.Add("force_mod2", br.Read(8));
                    Debug.WriteLine(MiniJson.ToString(vals));
                }
            Debug.WriteLine(MiniJson.ToString(obj));
            return;
        }
*/
    }
}
