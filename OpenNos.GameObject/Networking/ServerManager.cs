﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Core;
using OpenNos.Core.Networking.Communication.Scs.Communication.EndPoints.Tcp;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.WebApi.Reference;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.GameObject
{
    public class ServerManager : BroadcastableBase
    {
        #region Members

        public bool ShutdownStop;

        private static int seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> random =
                new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        private static ServerManager _instance;
        private static List<Item> _items = new List<Item>();

        private static ConcurrentDictionary<Guid, Map> _maps = new ConcurrentDictionary<Guid, Map>();

        private static List<NpcMonster> _npcs = new List<NpcMonster>();

        private static List<Skill> _skills = new List<Skill>();

        private bool _disposed;

        private List<DropDTO> _generalDrops;
        private ThreadSafeSortedList<long, Group> _groups;
        private ThreadSafeSortedList<long, Raid> _raids;
        private ThreadSafeSortedList<short, List<MapNpc>> _mapNpcs;
        private ThreadSafeSortedList<short, List<DropDTO>> _monsterDrops;
        private ThreadSafeSortedList<short, List<NpcMonsterSkill>> _monsterSkills;

        private ThreadSafeSortedList<int, List<Recipe>> _recipes;

        private ThreadSafeSortedList<int, List<ShopItemDTO>> _shopItems;
        private ThreadSafeSortedList<int, Shop> _shops;

        private ThreadSafeSortedList<int, List<ShopSkillDTO>> _shopSkills;
        private ThreadSafeSortedList<int, List<TeleporterDTO>> _teleporters;
        private long lastGroupId;
        private long lastRaidId;

        #endregion

        #region Instantiation

        private ServerManager()
        {
        }

        #endregion

        #region Properties

        public static int DropRate { get; set; }

        public static int FairyXpRate { get; set; }

        public static int GoldDropRate { get; set; }

        public static int GoldRate { get; set; }

        public static List<MailDTO> Mails { get; set; }

        public static int XPRate { get; set; }

        public int ChannelId { get; set; }

        public List<Group> Groups
        {
            get
            {
                return _groups.GetAllItems();
            }
        }

        public List<Raid> Raids
        {
            get
            {
                return _raids.GetAllItems();
            }
        }

        public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

        public Task TaskShutdown { get; set; }

        public Guid WorldId { get; set; }

        #endregion

        #region Methods

        public static ConcurrentDictionary<Guid, Map> GetAllMap()
        {
            return _maps;
        }

        public static IEnumerable<Skill> GetAllSkill()
        {
            return _skills;
        }

        public static int RandomNumber(int min = 0, int max = 100)
        {
            return random.Value.Next(min, max);
        }

        public static Item GetItem(short vnum)
        {
            return _items.FirstOrDefault(m => m.VNum.Equals(vnum));
        }

        public static Map GetMap(short id)
        {
            return _maps.FirstOrDefault(m => m.Value.MapId.Equals(id)).Value;
        }

        public static NpcMonster GetNpc(short npcVNum)
        {
            return _npcs.FirstOrDefault(m => m.NpcMonsterVNum.Equals(npcVNum));
        }

        public static Skill GetSkill(short skillVNum)
        {
            return _skills.FirstOrDefault(m => m.SkillVNum.Equals(skillVNum));
        }

        public void AddGroup(Group group)
        {
            _groups[group.GroupId] = group;
        }

        public void AddRaid(Raid raid)
        {
            _raids[raid.RaidId] = raid;
        }

        // PacketHandler -> with Callback?
        public void AskRevive(long characterId)
        {
            ClientSession Session = GetSessionByCharacterId(characterId);
            if (Session != null && Session.HasSelectedCharacter)
            {
                if (Session.Character.IsVehicled)
                {
                    Session.Character.RemoveVehicle();
                }
                Session.SendPacket(Session.Character.GenerateStat());
                Session.SendPacket(Session.Character.GenerateCond());
                Session.SendPackets(Session.Character.GenerateVb());
                if (Session.Character.Level > 20)
                {
                    Session.Character.Dignity -= (short)(Session.Character.Level < 50 ? Session.Character.Level : 50);
                    if (Session.Character.Dignity < -1000)
                    {
                        Session.Character.Dignity = -1000;
                    }
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LOSE_DIGNITY"), (short)(Session.Character.Level < 50 ? Session.Character.Level : 50)), 11));
                    Session.SendPacket(Session.Character.GenerateFd());
                    Session.CurrentMap?.Broadcast(Session, Session.Character.GenerateIn(), ReceiverType.AllExceptMe);
                }
                Session.SendPacket("eff_ob -1 -1 0 4269");
                Session.SendPacket(Session.Character.GenerateDialog($"#revival^0 #revival^1 {(Session.Character.Level > 20 ? Language.Instance.GetMessageFromKey("ASK_REVIVE") : Language.Instance.GetMessageFromKey("ASK_REVIVE_FREE"))}"));
                Task.Factory.StartNew(async () =>
                {
                    bool revive = true;
                    for (int i = 1; i <= 30; i++)
                    {
                        await Task.Delay(1000);
                        if (Session.Character.Hp > 0)
                        {
                            revive = false;
                            break;
                        }
                    }
                    if (revive)
                    {
                        Instance.ReviveFirstPosition(Session.Character.CharacterId);
                    }
                });
            }
        }

        // Both partly
        public void ChangeMap(long id, short? mapId = null, short? mapX = null, short? mapY = null)
        {
            ClientSession session = GetSessionByCharacterId(id);
            if (session?.Character != null && !session.Character.IsChangingMap)
            {
                try
                {
                    session.Character.IsChangingMap = true;

                    session.CurrentMap.RemoveMonstersTarget(session.Character.CharacterId);
                    session.CurrentMap.UnregisterSession(session.Character.CharacterId);

                    // cleanup sending queue to avoid sending uneccessary packets to it
                    session.ClearLowPriorityQueue();

                    // avoid cleaning new portals
                    if (mapId != null && mapX != null && mapY != null)
                    {
                        session.Character.MapId = (short)mapId;
                        session.Character.MapX = (short)mapX;
                        session.Character.MapY = (short)mapY;
                    }

                    session.CurrentMap = GetMap(session.Character.MapId);
                    session.CurrentMap.RegisterSession(session);
                    session.SendPacket(session.Character.GenerateCInfo());
                    session.SendPacket(session.Character.GenerateCMode());
                    session.SendPacket(session.Character.GenerateEq());
                    session.SendPacket(session.Character.GenerateEquipment());
                    session.SendPacket(session.Character.GenerateLev());
                    session.SendPacket(session.Character.GenerateStat());
                    session.SendPacket(session.Character.GenerateAt());
                    session.SendPacket(session.Character.GenerateCond());
                    session.SendPacket(session.Character.GenerateCMap());
                    session.SendPacket(session.Character.GenerateStatChar());
                    session.SendPacket($"gidx 1 {session.Character.CharacterId} -1 - 0"); // family
                    session.SendPacket("rsfp 0 -1");

                    // in 2 // send only when partner present cond 2 // send only when partner present
                    session.SendPacket(session.Character.GeneratePairy());
                    session.SendPacket("pinit 0"); // clear party list
                    session.SendPacket("act6"); // act6 1 0 14 0 0 0 14 0 0 0

                    Sessions.Where(s => s.Character != null && s.Character.MapId.Equals(session.Character.MapId) && s.Character.Name != session.Character.Name && !s.Character.InvisibleGm).ToList().ForEach(s => RequireBroadcastFromUser(session, s.Character.CharacterId, "GenerateIn"));

                    session.SendPackets(session.Character.GenerateGp());

                    // wp 23 124 4 4 12 99
                    session.SendPackets(session.Character.GenerateIn3());
                    session.SendPackets(session.Character.GenerateIn2());
                    session.SendPackets(session.Character.GenerateNPCShopOnMap());
                    session.SendPackets(session.Character.GenerateDroppedItem());
                    session.SendPackets(session.Character.GenerateShopOnMap());
                    session.SendPackets(session.Character.GeneratePlayerShopOnMap());
                    if (mapId == 138)
                    {
                        session.SendPacket("bc 0 0 0");
                    }
                    if (!session.Character.InvisibleGm)
                    {
                        session.CurrentMap?.Broadcast(session, session.Character.GenerateIn(), ReceiverType.AllExceptMe);
                    }
                    if (session.Character.Size != 10)
                    {
                        session.SendPacket(session.Character.GenerateScal());
                    }
                    if (session.CurrentMap != null && session.CurrentMap.IsDancing && !session.Character.IsDancing)
                    {
                        session.CurrentMap?.Broadcast("dance 2");
                    }
                    else if (session.CurrentMap != null && !session.CurrentMap.IsDancing && session.Character.IsDancing)
                    {
                        session.Character.IsDancing = false;
                        session.CurrentMap?.Broadcast("dance");
                    }
                    if (Groups != null)
                    {
                        foreach (Group g in Groups)
                        {
                            foreach (ClientSession groupSession in g.Characters)
                            {
                                ClientSession chara = Sessions.FirstOrDefault(s => s.Character != null && s.Character.CharacterId == groupSession.Character.CharacterId && s.CurrentMap.MapId == groupSession.CurrentMap.MapId);
                                if (chara != null)
                                {
                                    groupSession.SendPacket(groupSession.Character.GeneratePinit());
                                }
                                session.CurrentMap?.Broadcast(groupSession, groupSession.Character.GeneratePidx(), ReceiverType.AllExceptMe);
                            }
                        }
                    }

                    session.Character.IsChangingMap = false;
                }
                catch (Exception)
                {
                    Logger.Log.Warn("Character changed while changing map. Do not abuse Commands.");
                    session.Character.IsChangingMap = false;
                }
            }
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        public List<DropDTO> GetDropsByMonsterVNum(short monsterVNum)
        {
            return _monsterDrops.ContainsKey(monsterVNum) ? _generalDrops.Concat(_monsterDrops[monsterVNum]).ToList() : new List<DropDTO>();
        }

        public Group GetGroupByCharacterId(long characterId)
        {
            return Groups?.SingleOrDefault(g => g.IsMemberOfGroup(characterId));
        }

        public Raid GetRaidByCharacterId(long characterId)
        {
            return Raids?.SingleOrDefault(g => g.IsMemberOfRaid(characterId));
        }

        public long GetNextGroupId()
        {
            lastGroupId++;
            return lastGroupId;
        }

        public long GetNextRaidId()
        {
            lastRaidId++;
            return lastRaidId;
        }

        public T GetProperty<T>(string charName, string property)
        {
            ClientSession session = Sessions.FirstOrDefault(s => s.Character != null && s.Character.Name.Equals(charName));
            if (session == null)
            {
                return default(T);
            }
            return (T)session.Character.GetType().GetProperties().Single(pi => pi.Name == property).GetValue(session.Character, null);
        }

        public T GetProperty<T>(long charId, string property)
        {
            ClientSession session = GetSessionByCharacterId(charId);
            if (session == null)
            {
                return default(T);
            }
            return (T)session.Character.GetType().GetProperties().Single(pi => pi.Name == property).GetValue(session.Character, null);
        }

        public List<Recipe> GetReceipesByMapNpcId(int mapNpcId)
        {
            return _recipes.ContainsKey(mapNpcId) ? _recipes[mapNpcId] : new List<Recipe>();
        }

        public ClientSession GetSessionByCharacterName(string name)
        {
            return Sessions.SingleOrDefault(s => s.Character.Name == name);
        }

        public T GetUserMethod<T>(long characterId, string methodName)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session == null)
            {
                return default(T);
            }
            MethodInfo method = session.Character.GetType().GetMethod(methodName);

            return (T)method.Invoke(session.Character, null);
        }

        public void GroupLeave(ClientSession session)
        {
            if (Groups != null)
            {
                Group grp = Instance.Groups.FirstOrDefault(s => s.IsMemberOfGroup(session.Character.CharacterId));
                if (grp != null)
                {
                    if (grp.CharacterCount == 3)
                    {
                        if (grp.Characters.ElementAt(0) == session)
                        {
                            Broadcast(session, session.Character.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_LEADER")), ReceiverType.OnlySomeone, string.Empty, grp.Characters.ElementAt(1).Character.CharacterId);
                        }
                        grp.LeaveGroup(session);
                        foreach (ClientSession groupSession in grp.Characters)
                        {
                            ClientSession sess = GetSessionByCharacterId(groupSession.Character.CharacterId);
                            sess.SendPacket(sess.Character.GeneratePinit());
                            sess.SendPacket(sess.Character.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("LEAVE_GROUP"), session.Character.Name), 0));
                        }
                        session.SendPacket("pinit 0");
                        Broadcast(session.Character.GeneratePidx(true));
                        session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_LEFT"), 0));
                    }
                    else
                    {
                        foreach (ClientSession targetSession in grp.Characters)
                        {
                            targetSession.SendPacket("pinit 0");
                            targetSession.SendPacket(targetSession.Character.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_CLOSED"), 0));
                            Broadcast(targetSession.Character.GeneratePidx(true));
                            grp.LeaveGroup(targetSession);
                        }
                        RemoveGroup(grp);
                    }
                    session.Character.Group = null;
                }
            }
        }

        public void RaidLeave(ClientSession session)
        {
            if (Raids != null)
            {
                Raid raid = Instance.Raids.FirstOrDefault(s => s.IsMemberOfRaid(session.Character.CharacterId));
                if (raid != null)
                {
                    if (raid.CharacterCount > 1)
                    {
                        if (raid.Characters.ElementAt(0) == session)
                        {
                            Broadcast(session, session.Character.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_LEADER")), ReceiverType.OnlySomeone, string.Empty, raid.Characters.ElementAt(1).Character.CharacterId);
                        }
                        raid.LeaveGroup(session);
                        foreach (ClientSession groupSession in raid.Characters)
                        {
                            ClientSession sess = GetSessionByCharacterId(groupSession.Character.CharacterId);
                            sess.SendPacket(sess.Character.GeneratePinit());
                            sess.SendPacket(sess.Character.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("LEAVE_RAID"), session.Character.Name), 0));
                        }
                        session.SendPacket("pinit 0");
                        Broadcast(session.Character.GeneratePidx(true));
                        session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_LEFT"), 0));
                    }
                    else
                    {
                        RaidDisolve(session, raid);
                    }
                    session.Character.Raid = null;
                }
            }
        }

        public void RaidDisolve(ClientSession session, Raid raid = null)
        {
            if (raid == null)
                raid = Instance.Raids.FirstOrDefault(s => s.IsMemberOfRaid(session.Character.CharacterId));
            if (raid != null)
            {
                foreach (ClientSession targetSession in raid.Characters)
                {
                    targetSession.SendPacket("raid 1 0");
                    targetSession.SendPacket("raid 2 - 1");
                    targetSession.SendPacket(targetSession.Character.GenerateMsg(Language.Instance.GetMessageFromKey("RAID_CLOSED"), 0));
                    Broadcast(targetSession.Character.GeneratePidx(true));
                    raid.LeaveGroup(targetSession);
                }
                RemoveRaid(raid);
            }
        }

        public void Initialize(string ipAddress, int port)
        {
            // parse rates
            XPRate = int.Parse(System.Configuration.ConfigurationManager.AppSettings["RateXp"]);
            DropRate = int.Parse(System.Configuration.ConfigurationManager.AppSettings["RateDrop"]);
            GoldDropRate = int.Parse(System.Configuration.ConfigurationManager.AppSettings["GoldRateDrop"]);
            GoldRate = int.Parse(System.Configuration.ConfigurationManager.AppSettings["RateGold"]);
            FairyXpRate = int.Parse(System.Configuration.ConfigurationManager.AppSettings["RateFairyXp"]);

            Mails = DAOFactory.MailDAO.LoadAll().ToList();

            // load explicite type of ItemDTO
            foreach (ItemDTO itemDTO in DAOFactory.ItemDAO.LoadAll())
            {
                Item item;

                switch (itemDTO.ItemType)
                {
                    case ItemType.Ammo:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Armor:
                        item = new WearableItem(itemDTO);
                        break;

                    case ItemType.Box:
                        item = new BoxItem(itemDTO);
                        break;

                    case ItemType.Event:
                        item = new MagicalItem(itemDTO);
                        break;

                    case ItemType.Fashion:
                        item = new WearableItem(itemDTO);
                        break;

                    case ItemType.Food:
                        item = new FoodItem(itemDTO);
                        break;

                    case ItemType.Jewelery:
                        item = new WearableItem(itemDTO);
                        break;

                    case ItemType.Magical:
                        item = new MagicalItem(itemDTO);
                        break;

                    case ItemType.Main:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Map:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Part:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Potion:
                        item = new PotionItem(itemDTO);
                        break;

                    case ItemType.Production:
                        item = new ProduceItem(itemDTO);
                        break;

                    case ItemType.Quest1:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Quest2:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Sell:
                        item = new NoFunctionItem(itemDTO);
                        break;

                    case ItemType.Shell:
                        item = new MagicalItem(itemDTO);
                        break;

                    case ItemType.Snack:
                        item = new SnackItem(itemDTO);
                        break;

                    case ItemType.Special:
                        item = new SpecialItem(itemDTO);
                        break;

                    case ItemType.Specialist:
                        item = new WearableItem(itemDTO);
                        break;

                    case ItemType.Teacher:
                        item = new TeacherItem(itemDTO);
                        break;

                    case ItemType.Upgrade:
                        item = new UpgradeItem(itemDTO);
                        break;

                    case ItemType.Weapon:
                        item = new WearableItem(itemDTO);
                        break;

                    default:
                        item = new NoFunctionItem(itemDTO);
                        break;
                }
                _items.Add(item);
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("ITEMS_LOADED"), _items.Count));

            // intialize monsterdrops
            _monsterDrops = new ThreadSafeSortedList<short, List<DropDTO>>();
            foreach (var monsterDropGrouping in DAOFactory.DropDAO.LoadAll().GroupBy(d => d.MonsterVNum))
            {
                if (monsterDropGrouping.Key.HasValue)
                {
                    _monsterDrops[monsterDropGrouping.Key.Value] = monsterDropGrouping.OrderBy(d => d.DropChance).ToList();
                }
                else
                {
                    _generalDrops = monsterDropGrouping.ToList();
                }
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("DROPS_LOADED"), _monsterDrops.GetAllItems().Sum(i => i.Count)));

            // initialiize monsterskills
            _monsterSkills = new ThreadSafeSortedList<short, List<NpcMonsterSkill>>();
            foreach (var monsterSkillGrouping in DAOFactory.NpcMonsterSkillDAO.LoadAll().GroupBy(n => n.NpcMonsterVNum))
            {
                _monsterSkills[monsterSkillGrouping.Key] = monsterSkillGrouping.Select(n => n as NpcMonsterSkill).ToList();
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("MONSTERSKILLS_LOADED"), _monsterSkills.GetAllItems().Sum(i => i.Count)));

            // initialize npcmonsters
            foreach (NpcMonsterDTO npcmonsterDTO in DAOFactory.NpcMonsterDAO.LoadAll())
            {
                _npcs.Add(npcmonsterDTO as NpcMonster);
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("NPCMONSTERS_LOADED"), _npcs.Count));

            // intialize receipes
            _recipes = new ThreadSafeSortedList<int, List<Recipe>>();
            foreach (var recipeGrouping in DAOFactory.RecipeDAO.LoadAll().GroupBy(r => r.MapNpcId))
            {
                _recipes[recipeGrouping.Key] = recipeGrouping.Select(r => r as Recipe).ToList();
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("RECIPES_LOADED"), _recipes.GetAllItems().Sum(i => i.Count)));

            // initialize shopitems
            _shopItems = new ThreadSafeSortedList<int, List<ShopItemDTO>>();
            foreach (var shopItemGrouping in DAOFactory.ShopItemDAO.LoadAll().GroupBy(s => s.ShopId))
            {
                _shopItems[shopItemGrouping.Key] = shopItemGrouping.ToList();
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("SHOPITEMS_LOADED"), _shopItems.GetAllItems().Sum(i => i.Count)));

            // initialize shopskills
            _shopSkills = new ThreadSafeSortedList<int, List<ShopSkillDTO>>();
            foreach (var shopSkillGrouping in DAOFactory.ShopSkillDAO.LoadAll().GroupBy(s => s.ShopId))
            {
                _shopSkills[shopSkillGrouping.Key] = shopSkillGrouping.ToList();
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("SHOPSKILLS_LOADED"), _shopSkills.GetAllItems().Sum(i => i.Count)));

            // initialize shops
            _shops = new ThreadSafeSortedList<int, Shop>();
            foreach (var shopGrouping in DAOFactory.ShopDAO.LoadAll())
            {
                _shops[shopGrouping.MapNpcId] = (Shop)shopGrouping;
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("SHOPS_LOADED"), _shops.GetAllItems().Count));

            // initialize teleporters
            _teleporters = new ThreadSafeSortedList<int, List<TeleporterDTO>>();
            foreach (var teleporterGrouping in DAOFactory.TeleporterDAO.LoadAll().GroupBy(t => t.MapNpcId))
            {
                _teleporters[teleporterGrouping.Key] = teleporterGrouping.Select(t => t).ToList();
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("TELEPORTERS_LOADED"), _teleporters.GetAllItems().Sum(i => i.Count)));

            // initialize skills
            foreach (SkillDTO skillDTO in DAOFactory.SkillDAO.LoadAll())
            {
                Skill skill = (Skill)skillDTO;
                skill.Combos.AddRange(DAOFactory.ComboDAO.LoadBySkillVnum(skill.SkillVNum).ToList());
                _skills.Add(skill);
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("SKILLS_LOADED"), _skills.Count));

            // intialize mapnpcs
            _mapNpcs = new ThreadSafeSortedList<short, List<MapNpc>>();
            foreach (var mapNpcGrouping in DAOFactory.MapNpcDAO.LoadAll().GroupBy(t => t.MapId))
            {
                _mapNpcs[mapNpcGrouping.Key] = mapNpcGrouping.Select(t => t as MapNpc).ToList();
            }
            Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("MAPNPCS_LOADED"), _mapNpcs.GetAllItems().Sum(i => i.Count)));

            try
            {
                int i = 0;
                int monstercount = 0;

                foreach (MapDTO map in DAOFactory.MapDAO.LoadAll())
                {
                    Guid guid = Guid.NewGuid();
                    Map newMap = new Map(map.MapId, guid, map.Data)
                    {
                        Music = map.Music,
                        ShopAllowed = map.ShopAllowed
                    };

                    // register for broadcast
                    _maps.TryAdd(guid, newMap);
                    newMap.SetMapMapMonsterReference();
                    newMap.SetMapMapNpcReference();
                    i++;

                    newMap.LoadMonsters();
                    foreach (MapMonster mapMonster in newMap.Monsters)
                    {
                        mapMonster.Map = newMap;
                        newMap.AddMonster(mapMonster);
                    }
                    monstercount += newMap.Monsters.Count;
                }
                if (i != 0)
                {
                    Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("MAPS_LOADED"), i));
                }
                else
                {
                    Logger.Log.Error(Language.Instance.GetMessageFromKey("NO_MAP"));
                }
                Logger.Log.Info(string.Format(Language.Instance.GetMessageFromKey("MAPMONSTERS_LOADED"), monstercount));
            }
            catch (Exception ex)
            {
                Logger.Log.Error("General Error", ex);
            }
            LaunchEvents();

            //Register the new created TCPIP server to the api
            Guid serverIdentification = Guid.NewGuid();
            WorldId = serverIdentification;
        }

        public bool IsCharacterMemberOfGroup(long characterId)
        {
            return Groups != null && Groups.Any(g => g.IsMemberOfGroup(characterId));
        }

        public bool IsCharactersGroupFull(long characterId)
        {
            return Groups != null && Groups.Any(g => g.IsMemberOfGroup(characterId) && g.CharacterCount == 3);
        }

        // Server
        public bool Kick(string characterName)
        {
            ClientSession session = Sessions.FirstOrDefault(s => s.Character != null && s.Character.Name.Equals(characterName));
            if (session == null)
            {
                return false;
            }
            session.Disconnect();
            return true;
        }

        public void LaunchEvents()
        {
            _groups = new ThreadSafeSortedList<long, Group>();
            _raids = new ThreadSafeSortedList<long, Raid>();

            Observable.Interval(TimeSpan.FromMinutes(5)).Subscribe(x =>
            {
                SaveAllProcess();
            });

            Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(x =>
            {
                GroupProcess();
            });

            Observable.Interval(TimeSpan.FromHours(3)).Subscribe(x =>
            {
                BotProcess();
            });

            Observable.Interval(TimeSpan.FromSeconds(30)).Subscribe(x =>
            {
                MailProcess();
            });

            Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x =>
            {
                RemoveItemProcess();
            });

            foreach (var map in _maps)
            {
                Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(x =>
                {
                    try
                    {
                        if (!map.Value.IsSleeping)
                        {
                            map.Value.RemoveMapItem();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                });

                foreach (MapNpc npc in map.Value.Npcs)
                {
                    npc.StartLife();
                }

                Observable.Interval(TimeSpan.FromMilliseconds(400)).Subscribe(x =>
                {
                    Parallel.ForEach(map.Value.Monsters, monster => { monster.StartLife(); });
                });
            }

            ServerCommunicationClient.Instance.SessionKickedEvent += OnSessionKicked;
            ServerCommunicationClient.Instance.MessageSentToCharacter += OnMessageSentToCharacter;

            lastGroupId = 1;
            lastRaidId = 1;
        }

        // Map
        public void LeaveMap(long id)
        {
            ClientSession session = GetSessionByCharacterId(id);
            if (session == null)
            {
                return;
            }
            session.SendPacket(session.Character.GenerateAt());
            session.SendPacket(session.Character.GenerateCMap());
            session.SendPacket(session.Character.GenerateMapOut());
            session.CurrentMap?.Broadcast(session, session.Character.GenerateOut(), ReceiverType.AllExceptMe);
        }

        public void RequireBroadcastFromUser(ClientSession client, long characterId, string methodName)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session == null)
            {
                return;
            }
            MethodInfo method = session.Character.GetType().GetMethod(methodName);
            string result = (string)method.Invoke(session.Character, null);
            client.SendPacket(result);
        }

        // Map
        public void ReviveFirstPosition(long characterId)
        {
            ClientSession session = GetSessionByCharacterId(characterId);
            if (session != null && session.Character.Hp <= 0)
            {
                Random rnd = new Random();
                LeaveMap(session.Character.CharacterId);
                session.Character.Hp = 1;
                session.Character.Mp = 1;
                RespawnMapTypeDTO resp = session.Character.Respawn;
                short x = (short)(resp.DefaultX + RandomNumber(-5, 5));
                short y = (short)(resp.DefaultY + RandomNumber(-5, 5));
                ChangeMap(session.Character.CharacterId, resp.DefaultMapId, x, y);
                session.CurrentMap?.Broadcast(session, session.Character.GenerateTp());
                session.CurrentMap?.Broadcast(session.Character.GenerateRevive());
                session.SendPacket(session.Character.GenerateStat());
            }
        }

        public void SaveAll()
        {
            List<ClientSession> sessions = Sessions.Where(c => c.IsConnected).ToList();
            sessions.ForEach(s => s.Character?.Save());
        }

        public void SetProperty(long charId, string property, object value)
        {
            ClientSession session = GetSessionByCharacterId(charId);
            if (session == null)
            {
                return;
            }
            PropertyInfo propertyinfo = session.Character.GetType().GetProperties().Single(pi => pi.Name == property);
            propertyinfo.SetValue(session.Character, value, null);
        }

        public void Shout(string message)
        {
            Broadcast($"say 1 0 10 ({Language.Instance.GetMessageFromKey("ADMINISTRATOR")}){message}");
            Broadcast($"msg 2 {message}");
        }

        // Server
        public void UpdateGroup(long charId)
        {
            try
            {
                if (Groups != null)
                {
                    Group myGroup = Groups.FirstOrDefault(s => s.IsMemberOfGroup(charId));
                    if (myGroup == null)
                    {
                        return;
                    }
                    string str = $"pinit {myGroup.Characters.Count}";
                    int i = 0;
                    IList<ClientSession> groupMembers = Groups.FirstOrDefault(s => s.IsMemberOfGroup(charId))?.Characters;
                    if (groupMembers != null)
                    {
                        foreach (ClientSession session in groupMembers)
                        {
                            i++;
                            str += $" 1|{session.Character.CharacterId}|{i}|{session.Character.Level}|{session.Character.Name}|11|{(byte)session.Character.Gender}|{(byte)session.Character.Class}|{(session.Character.UseSp ? session.Character.Morph : 0)}|{(session.Character.IsVehicled ? 1 : 0)}|{session.Character.HeroLevel}";
                        }
                    }

                    foreach (ClientSession session in myGroup.Characters)
                    {
                        session.SendPacket(str);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal IEnumerable<MapNpc> GetMapNpcsByMapId(short mapId)
        {
            if (_mapNpcs.ContainsKey(mapId))
            {
                return _mapNpcs[mapId];
            }

            return new List<MapNpc>();
        }

        internal List<NpcMonsterSkill> GetNpcMonsterSkillsByMonsterVNum(short npcMonsterVNum)
        {
            if (_monsterSkills.ContainsKey(npcMonsterVNum))
            {
                return _monsterSkills[npcMonsterVNum];
            }

            return new List<NpcMonsterSkill>();
        }

        internal Shop GetShopByMapNpcId(int mapNpcId)
        {
            if (_shops.ContainsKey(mapNpcId))
            {
                return _shops[mapNpcId];
            }

            return null;
        }

        internal List<ShopItemDTO> GetShopItemsByShopId(int shopId)
        {
            if (_shopItems.ContainsKey(shopId))
            {
                return _shopItems[shopId];
            }

            return new List<ShopItemDTO>();
        }

        internal List<ShopSkillDTO> GetShopSkillsByShopId(int shopId)
        {
            if (_shopSkills.ContainsKey(shopId))
            {
                return _shopSkills[shopId];
            }

            return new List<ShopSkillDTO>();
        }

        internal List<TeleporterDTO> GetTeleportersByNpcVNum(short npcMonsterVNum)
        {
            if (_teleporters != null && _teleporters.ContainsKey(npcMonsterVNum))
            {
                return _teleporters[npcMonsterVNum];
            }
            else
            {
                return new List<TeleporterDTO>();
            }
        }

        internal void StopServer()
        {
            Instance.ShutdownStop = true;
            Instance.TaskShutdown = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _monsterDrops.Dispose();
                _groups.Dispose();
                _raids.Dispose();
                _monsterSkills.Dispose();
                _shopSkills.Dispose();
                _shopItems.Dispose();
                _shops.Dispose();
                _recipes.Dispose();
                _mapNpcs.Dispose();
                _teleporters.Dispose();
            }
        }

        // Server
        private void BotProcess()
        {
            try
            {
                Shout(Language.Instance.GetMessageFromKey($"BOT_MESSAGE_{ RandomNumber(0, 5) }"));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void GroupProcess()
        {
            try
            {
                if (Groups != null)
                {
                    foreach (Group grp in Groups)
                    {
                        foreach (ClientSession session in grp.Characters)
                        {
                            foreach (string str in grp.GeneratePst())
                            {
                                session.SendPacket(str);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void MailProcess()
        {
            try
            {
                Mails = DAOFactory.MailDAO.LoadAll().ToList();
                Sessions.Where(c => c.IsConnected).ToList().ForEach(s => s.Character?.RefreshMail());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void OnMessageSentToCharacter(object sender, EventArgs e)
        {
            if (sender != null)
            {
                Tuple<string, string, int, MessageType> message = (Tuple<string, string, int, MessageType>)sender;

                ClientSession targetSession = Sessions.SingleOrDefault(s => s.Character.Name == message.Item1);

                if (targetSession != null || message.Item4 == MessageType.Shout) //shout doesnt need targetSession
                {
                    switch (message.Item4)
                    {
                        case MessageType.Whisper:
                            {
                                targetSession.SendPacket(String.Format("{0} <Channel: {1}>", message.Item2, message.Item3));
                                break;
                            }
                        case MessageType.Shout:
                            {
                                Shout(message.Item2);
                                break;
                            }
                        case MessageType.PrivateChat:
                            {
                                targetSession.SendPacket(message.Item2);
                                break;
                            }
                    }
                }
            }
        }

        private void OnSessionKicked(object sender, EventArgs e)
        {
            if (sender != null)
            {
                Tuple<long?, string> kickedSession = (Tuple<long?, string>)sender;

                ClientSession targetSession = Sessions.FirstOrDefault(s => (!kickedSession.Item1.HasValue || s.SessionId == kickedSession.Item1.Value)
                                                        && ((String.IsNullOrEmpty(kickedSession.Item2) || s.Account.Name == kickedSession.Item2)));

                if (targetSession != null)
                {
                    targetSession.Disconnect();
                }
            }
        }

        private void RemoveGroup(Group grp)
        {
            _groups.Remove(grp.GroupId);
        }

        private void RemoveRaid(Raid raid)
        {
            _raids.Remove(raid.RaidId);
        }

        private void RemoveItemProcess()
        {
            try
            {
                Sessions.Where(c => c.IsConnected).ToList().ForEach(s => s.Character?.RefreshValidity());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        // Server
        private void SaveAllProcess()
        {
            try
            {
                Logger.Log.Info(Language.Instance.GetMessageFromKey("SAVING_ALL"));
                SaveAll();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        #endregion
    }
}