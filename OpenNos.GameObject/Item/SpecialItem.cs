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
using OpenNos.Data;
using OpenNos.Domain;
using System.Linq;

namespace OpenNos.GameObject
{
    public class SpecialItem : Item
    {
        #region Instantiation

        public SpecialItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        public override void Use(ClientSession session, ref ItemInstance inv, bool delay = false, string[] packetsplit = null)
        {
            switch (Effect)
            {
                // sp point potions
                case 150:
                case 151:
                    session.Character.SpAdditionPoint += EffectValue;
                    if (session.Character.SpAdditionPoint > 1000000)
                    {
                        session.Character.SpAdditionPoint = 1000000;
                    }
                    session.SendPacket(session.Character.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDED"), EffectValue), 0));
                    session.Character.Inventory.RemoveItemAmountFromInventory(1, inv.Id);
                    session.SendPacket(session.Character.GenerateSpPoint());
                    break;

                case 204:
                    session.Character.SpPoint += EffectValue;
                    session.Character.SpAdditionPoint += EffectValue * 3;
                    if (session.Character.SpAdditionPoint > 1000000)
                    {
                        session.Character.SpAdditionPoint = 1000000;
                    }
                    if (session.Character.SpPoint > 10000)
                    {
                        session.Character.SpPoint = 10000;
                    }
                    session.SendPacket(session.Character.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("SP_POINTSADDEDBOTH"), EffectValue, EffectValue * 3), 0));
                    session.Character.Inventory.RemoveItemAmountFromInventory(1, inv.Id);
                    session.SendPacket(session.Character.GenerateSpPoint());
                    break;

                /*********************************\
                **             Sceaux            **
                ***********************************
                ** VNUM :
                ** - 1127 : Cuby
                ** - 1128 : Ginseng
                ** - 1129 : Castra
                ** - 1130 : Jack
                ** - 1131 : Slade
                \**********************************/
                case 301:
                    // Delete item
                    //session.Character.Inventory.RemoveItemAmountFromInventory(1, inv.Id);
                    // Create raid
                    Raid raid = new Raid();
                    raid.JoinRaid(session);
                    session.SendPacket($"raid 2 {session.SessionId}");
                    session.SendPacket($"raid 0 {session.SessionId}");
                    session.SendPacket("raid 1 1");
                    session.SendPacket(raid.GenerateRdlst());
                    session.SendPacket($"say 1 {session.SessionId} 10 Tu es chef de raid à présent. Invite des membres.");
                    session.SendPacket($"msg 0 Tu es chef de raid à présent. Invite des membres.");
                    ServerManager.Instance.AddRaid(raid);
                    break;

                // Divorce letter
                case 6969: // this is imaginary number I = √(-1)
                    break;

                // Cupid's arrow
                case 34: // this is imaginary number I = √(-1)
                    break;

                // wings
                case 650:
                    SpecialistInstance specialistInstance = session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (session.Character.UseSp && specialistInstance != null)
                    {
                        if (!delay)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_WINGS_CHANGE")}");
                        }
                        else
                        {
                            specialistInstance.Design = (byte)EffectValue;
                            session.Character.MorphUpgrade2 = EffectValue;
                            session.CurrentMap?.Broadcast(session.Character.GenerateCMode());
                            session.SendPacket(session.Character.GenerateStat());
                            session.SendPacket(session.Character.GenerateStatChar());
                            session.Character.Inventory.RemoveItemAmountFromInventory(1, inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("NO_SP"), 0));
                    }
                    break;

                // presentation messages
                case 203:
                    if (!session.Character.IsVehicled)
                    {
                        if (!delay)
                        {
                            session.SendPacket(session.Character.GenerateGuri(10, 2, 1));
                        }
                    }
                    break;

                // magic lamps
                case 651:
                    if (session.Character.Inventory.GetAllItems().All(i => i.Type != InventoryType.Wear))
                    {
                        if (!delay)
                        {
                            session.SendPacket($"qna #u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^3 {Language.Instance.GetMessageFromKey("ASK_USE")}");
                        }
                        else
                        {
                            session.Character.ChangeSex();
                            session.Character.Inventory.RemoveItemAmountFromInventory(1, inv.Id);
                        }
                    }
                    else
                    {
                        session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("EQ_NOT_EMPTY"), 0));
                    }
                    break;

                // vehicles
                case 1000:
                    if (Morph > 0)
                    {
                        if (!delay && !session.Character.IsVehicled)
                        {
                            if (session.Character.IsSitting)
                            {
                                session.Character.IsSitting = false;
                                session.CurrentMap?.Broadcast(session.Character.GenerateRest());
                            }
                            session.SendPacket(session.Character.GenerateDelay(3000, 3, $"#u_i^1^{session.Character.CharacterId}^{(byte)inv.Type}^{inv.Slot}^2"));
                        }
                        else
                        {
                            if (!session.Character.IsVehicled)
                            {
                                session.Character.Speed = Speed;
                                session.Character.IsVehicled = true;
                                session.Character.VehicleSpeed = Speed;
                                session.Character.MorphUpgrade = 0;
                                session.Character.MorphUpgrade2 = 0;
                                session.Character.Morph = Morph + (byte)session.Character.Gender;
                                session.CurrentMap?.Broadcast(session.Character.GenerateEff(196), session.Character.MapX, session.Character.MapY);
                                session.CurrentMap?.Broadcast(session.Character.GenerateCMode());
                                session.SendPacket(session.Character.GenerateCond());
                            }
                            else
                            {
                                session.Character.RemoveVehicle();
                            }
                        }
                    }
                    break;

                case 69:
                    session.Character.Reput += ReputPrice;
                    session.SendPacket(session.Character.GenerateFd());
                    session.Character.Inventory.RemoveItemAmountFromInventory(1, inv.Id);
                    break;

                default:
                    Logger.Log.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType()));
                    Logger.Log.Warn($"ID : {Effect}");
                    break;
            }
        }

        #endregion
    }
}