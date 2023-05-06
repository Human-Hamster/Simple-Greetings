using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using SimpleGreetings.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGreetings.GameData
{
    internal class ConditionCheck
    {
        private TerritoryHandler terHandler;
        private ClientState cState;
        public ConditionCheck(TerritoryHandler th, ClientState cState)
        {
            this.terHandler = th;
            this.cState = cState;
        }

        public Boolean isRoulette()
        {
            return false;
        }
        //public ContentType getContentType()
        //{
        //}
        //
        //public Boolean AtLeastOnePartyMemberJoined()
        //{
        //}
    }
}

