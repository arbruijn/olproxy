using System.Collections.Generic;
using System.Linq;
using minijson;

namespace olproxy
{
    using MJDict = Dictionary<string, object>;
    using MJList = List<object>;

    class MatchInfo
    {
        private string Creator;
        private string Level;
        private bool IsTeams;
        private bool HasMatchData;

        public PrivateMatchData PrivateMatchData { get; }
        public int PlayerCount { get; }

        public override string ToString()
        {
            return (HasMatchData ? (IsTeams ? "Teams, " : "") + Level + ", by " + Creator + ", " : "") + PlayerCount +
                (HasMatchData ? "/" + PrivateMatchData.MaxPlayers : "") +
                " players";
        }

        public MatchInfo(string matchMessage)
        {
            var msg = MiniJson.Parse(matchMessage);
            var attr = (msg as MJDict)["attr"] as MJDict;
            if (!attr.TryGetValue("mm_players", out object plVal) && !attr.TryGetValue("mm_mmPlayers", out plVal))
                return;
            var players = MiniJson.Parse((plVal as MJDict)["S"] as string) as MJList;
            PlayerCount = players.Count;
            foreach (var pl in players)
                if (((pl as MJDict)["PlayerAttributes"] as MJDict).TryGetValue("private_match_data", out object data))
                {
                    PrivateMatchData = new PrivateMatchData(System.Convert.FromBase64String((data as MJDict)["valueAttribute"] as string));
                    HasMatchData = true;
                    Creator = PrivateMatchData.Creator;
                    Level = PrivateMatchData.LevelName;
                    IsTeams = PrivateMatchData.Mode == 1;
                }
        }
    }
}
