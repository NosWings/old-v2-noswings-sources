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
        private bool _disposed;
        private int _order;
        short _min_lvl;
        short _max_lvl;
        short _ave_lvl;

        #endregion

        #region Instantiation

        public Raid(short min_lvl = 20, short max_lvl = 99)
        {
            _characters = new ThreadSafeSortedList<long, ClientSession>();
            _order = 0;
            _min_lvl = min_lvl;
            _max_lvl = max_lvl;
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

        #endregion

        #region Methods

        public string GenerateRdlst()
        {
            string result = $"rdlst {_min_lvl} {_ave_lvl} 0 ";

            foreach (ClientSession session in Characters)
            {
                result += $"{session.Character.Level}."
                    + $"{(session.Character.UseSp || session.Character.IsVehicled ? session.Character.Morph : 0)}."
                    + $"{(short)session.Character.Class}.0.{session.Character.Name}.0."
                    + $"{session.SessionId}.{session.Character.HeroLevel} ";
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
        
        public long? GetNextOrderedCharacterId(Character character)
        {
            lock (this)
            {
                _order++;
                List<ClientSession> sessions = Characters.Where(s => Map.GetDistance(s.Character, character) < 50).ToList();
                if (_order > sessions.Count - 1) // if order wents out of amount of ppl, reset it -> zero based index
                {
                    _order = 0;
                }

                if (!sessions.Any()) // group seems to be empty
                {
                    return null;
                }

                return sessions[_order].Character.CharacterId;
            }
        }

        public bool IsMemberOfRaid(long characterId)
        {
            return _characters != null && _characters.ContainsKey(characterId);
        }

        public bool IsMemberOfRaid(ClientSession session)
        {
            return _characters != null && _characters.ContainsKey(session.Character.CharacterId);
        }

        public void JoinRaid(long characterId)
        {
            ClientSession session = ServerManager.Instance.GetSessionByCharacterId(characterId);
            if (session != null)
            {
                JoinRaid(session);
            }
        }

        public void JoinRaid(ClientSession session)
        {
            session.Character.Raid = this;
            _characters[session.Character.CharacterId] = session;
        }

        public void LeaveGroup(ClientSession session)
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

        #endregion
    }
}
