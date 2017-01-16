using System;
using System.Linq;
using OpenNos.Core;
using System.Collections.Generic;

namespace OpenNos.GameObject
{
    public class Raid
    {
        #region Members
        
        private ThreadSafeSortedList<long, ClientSession> _characters;
        private ClientSession _leader;
        private bool _disposed;
        private short _min_lvl;
        private short _max_lvl;
        private short _ave_lvl;
        private short _max_players;

        #endregion

        #region Instantiation

        public Raid(short min_lvl = 20, short max_lvl = 99, short max_players = 15)
        {
            _characters = new ThreadSafeSortedList<long, ClientSession>();
            _leader = null;
            _min_lvl = min_lvl;
            _max_lvl = max_lvl;
            _max_players = max_players;
            _ave_lvl = (short)(max_lvl + 10 > 99 ? 99 : max_lvl + 10); // lvl_max_drop + 10
            RaidId = ServerManager.Instance.GetNextRaidId();
        }

        #endregion

        #region Properties

        public long RaidId { get; set; }

        public int CharacterCount
        {
            get
            {
                return _characters.Count;
            }
        }

        public List<ClientSession> Characters
        {
            get
            {
                return _characters.GetAllItems();
            }
        }

        public short MinLvl
        {
            get
            {
                return _min_lvl;
            }
        }

        public short AverageLvl
        {
            get
            {
                return _ave_lvl;
            }
        }

        public ClientSession Leader
        {
            get
            {
                return _leader;
            }
            set
            {
                _leader = value;
            }
        }

        #endregion

        #region Methods

        public string GenerateRdlst()
        {
            ClientSession session;
            string result = $"rdlst {_min_lvl} {_ave_lvl} 0 ";

            result += $"{_leader.Character.Level}."
                    + $"{(_leader.Character.UseSp || _leader.Character.IsVehicled ? _leader.Character.Morph : 0)}."
                    + $"{(short)_leader.Character.Class}.0.{_leader.Character.Name}.0."
                    + $"{_leader.Character.CharacterId}.{_leader.Character.HeroLevel} ";

            for (int i = 0; i < CharacterCount; i++)
            {
                session = Characters.ElementAt(i);
                result += $"{session.Character.Level}."
                    + $"{(session.Character.UseSp || session.Character.IsVehicled ? session.Character.Morph : 0)}."
                    + $"{(short)session.Character.Class}.0.{session.Character.Name}.0."
                    + $"{session.Character.CharacterId}.{session.Character.HeroLevel} ";
            }
            return result.Remove(result.Length - 1);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
        
        public bool IsMemberOfRaid(long characterId)
        {
            return _characters != null && (_characters.ContainsKey(characterId) || _leader.Character.CharacterId == characterId);
        }

        public bool IsMemberOfRaid(ClientSession session)
        {
            return _characters != null && (_characters.ContainsKey(session.Character.CharacterId) || session == _leader);
        }

        public void JoinRaid(long characterId)
        {
            ClientSession session = ServerManager.Instance.GetSessionByCharacterId(characterId);
            if (session != null)
                JoinRaid(session);
        }

        public void JoinRaid(ClientSession session)
        {
            session.Character.Raid = this;
            if (_leader == null)
                _leader = session;
            else
                _characters[session.Character.CharacterId] = session;
            UpdateVisual();
        }

        public void KickPlayerFromRaid(long characterId)
        {
            ClientSession session = ServerManager.Instance.GetSessionByCharacterId(characterId);
            if (session != null)
                KickPlayerFromRaid(session);
        }

        public void KickPlayerFromRaid(ClientSession session)
        {
            session.Character.Raid = null;
            _characters.Remove(session.Character.CharacterId);
        }

        public void LeaveRaid(ClientSession session)
        {
            session.Character.Raid = null;
            _characters.Remove(session.Character.CharacterId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _characters.Dispose();
            }
        }

        public void UpdateVisual()
        {
            _leader.SendPacket(GenerateRdlst());
            foreach (ClientSession client in this.Characters)
            {
                client.SendPacket(GenerateRdlst());
            }
        }

        #endregion
    }
}
