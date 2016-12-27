/*
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

using System;
using OpenNos.Core;
using OpenNos.Data;
using OpenNos.Domain;
using System.Collections.Generic;

namespace OpenNos.GameObject
{
    public class BoxItem : Item
    {
        #region Instantiation

        public BoxItem(ItemDTO item) : base(item)
        {
        }

        #endregion

        #region Methods

        class S_RaidBoxItem
        {
            public int amount;
            public short itemvnum;
            public float drop_chance; // <= 100.00

            public S_RaidBoxItem(int amount, short itemvnum, float drop_chance)
            {
                this.amount = amount;
                this.itemvnum = itemvnum;
                this.drop_chance = drop_chance;
            }
        }

        private short gen_random_id(List<S_RaidBoxItem> items)
        {
            List<S_RaidBoxItem> tmp = new List<S_RaidBoxItem>();

            foreach (S_RaidBoxItem item in items)
                for (int i = 0; i < item.drop_chance; i++)
                    tmp.Add(item);

            return tmp[new Random().Next(tmp.Count)].itemvnum;
        }

        List<S_RaidBoxItem> cuby_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(10, 265, 20),
            new S_RaidBoxItem(50, 2282, 50), // Plume d'ange
            new S_RaidBoxItem(30, 1030, 30)  // Pleine lune
        };

        public override void Use(ClientSession session, ref ItemInstance inv, bool delay = false, string[] packetsplit = null)
        {
            switch (Effect)
            {
                case 0:
                    if (VNum == 302)
                    {
                        BoxInstance raidBox = session.Character.Inventory.LoadBySlotAndType<BoxInstance>(inv.Slot, InventoryType.Equipment);
                        ItemInstance newInv;
                        byte numberOfItem = 1;
                        switch (raidBox.Design)
                        {
                            case 1: //XYXY
                                newInv = session.Character.Inventory.AddNewToInventory(289, numberOfItem);
                                break;
                            case 2: //CASTRA
                                newInv = session.Character.Inventory.AddNewToInventory(289, numberOfItem);
                                break;
                            case 3: //JACK
                                newInv = session.Character.Inventory.AddNewToInventory(289, numberOfItem);
                                break;
                            case 4: //SLADE
                                newInv = session.Character.Inventory.AddNewToInventory(289, numberOfItem);
                                break;
                            case 9: //IBRAHIM
                                numberOfItem = 50;
                                newInv = session.Character.Inventory.AddNewToInventory(2282, numberOfItem);
                                break;
                            case 13: //KERTOS
                                numberOfItem = 30;
                                newInv = session.Character.Inventory.AddNewToInventory(2282, numberOfItem);
                                break;
                            case 14: //VALAKUS
                                numberOfItem = 40;
                                newInv = session.Character.Inventory.AddNewToInventory(2282, numberOfItem);
                                break;
                            case 15: //GRENIGAS
                                numberOfItem = 50;
                                newInv = session.Character.Inventory.AddNewToInventory(2282, numberOfItem);
                                break;
                            case 16: //DRACO
                                numberOfItem = 10;
                                newInv = session.Character.Inventory.AddNewToInventory(2282, numberOfItem);
                                break;
                            case 17: //GLAGLA
                                numberOfItem = 20;
                                newInv = session.Character.Inventory.AddNewToInventory(2282, numberOfItem);
                                break;
                            default: //CUBY
                                newInv = session.Character.Inventory.AddNewToInventory(gen_random_id(cuby_loots), numberOfItem);
                                break;
                        }
                        if (newInv != null)
                        {
                            dynamic raidBoxItem;
                            if (newInv.Type == InventoryType.Equipment)
                            {
                                newInv.Rare = raidBox.Rare;
                                raidBoxItem =
                                    session.Character.Inventory.LoadBySlotAndType<WearableInstance>(newInv.Slot,
                                        newInv.Type);
                                if (raidBoxItem != null &&
                                    (raidBoxItem.Item.EquipmentSlot == EquipmentType.Armor ||
                                     raidBoxItem.Item.EquipmentSlot == EquipmentType.MainWeapon ||
                                     raidBoxItem.Item.EquipmentSlot == EquipmentType.SecondaryWeapon))
                                {
                                    raidBoxItem.SetRarityPoint();
                                }
                            }
                            else
                            {
                                raidBoxItem = session.Character.Inventory.LoadBySlotAndType<ItemInstance>(newInv.Slot, newInv.Type);
                            }

                            short Slot = inv.Slot;
                            if (Slot != -1)
                            {
                                session.SendPacket($"rdi {raidBoxItem.Item.VNum} {numberOfItem}");
                                session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {raidBoxItem.Item.Name}", 12));
                                session.SendPacket(session.Character.GenerateInventoryAdd(raidBoxItem.ItemVNum, newInv.Amount, raidBoxItem.Type, newInv.Slot, raidBoxItem.Rare, 0, 0, 0));
                                session.Character.Inventory.RemoveItemAmountFromInventory(1, raidBox.Id);
                            }
                        }
                    }
                    break;
                case 69:
                    if (EffectValue == 1 || EffectValue == 2)
                    {
                        BoxInstance box = session.Character.Inventory.LoadBySlotAndType<BoxInstance>(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"wopen 44 {inv.Slot}");
                            }
                            else
                            {
                                ItemInstance newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv != null)
                                {
                                    SpecialistInstance specialist = session.Character.Inventory.LoadBySlotAndType<SpecialistInstance>(newInv.Slot, newInv.Type);

                                    if (specialist != null)
                                    {
                                        specialist.SlDamage = box.SlDamage;
                                        specialist.SlDefence = box.SlDefence;
                                        specialist.SlElement = box.SlElement;
                                        specialist.SlHP = box.SlHP;
                                        specialist.SpDamage = box.SpDamage;
                                        specialist.SpDark = box.SpDark;
                                        specialist.SpDefence = box.SpDefence;
                                        specialist.SpElement = box.SpElement;
                                        specialist.SpFire = box.SpFire;
                                        specialist.SpHP = box.SpHP;
                                        specialist.SpLevel = box.SpLevel;
                                        specialist.SpLight = box.SpLight;
                                        specialist.SpStoneUpgrade = box.SpStoneUpgrade;
                                        specialist.SpWater = box.SpWater;
                                        specialist.Upgrade = box.Upgrade;
                                        specialist.XP = box.XP;
                                    }

                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {specialist.Item.Name} + {specialist.Upgrade}", 12));
                                        session.SendPacket(session.Character.GenerateInventoryAdd(specialist.ItemVNum, newInv.Amount, specialist.Type, newInv.Slot, 0, 0, specialist.Upgrade, 0));
                                        session.Character.Inventory.RemoveItemAmountFromInventory(1, box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }
                    if (EffectValue == 3)
                    {
                        BoxInstance box = session.Character.Inventory.LoadBySlotAndType<BoxInstance>(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"guri 26 0 {inv.Slot}");
                            }
                            else
                            {
                                ItemInstance newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv != null)
                                {
                                    WearableInstance fairy = session.Character.Inventory.LoadBySlotAndType<WearableInstance>(newInv.Slot, newInv.Type);

                                    if (fairy != null)
                                    {
                                        fairy.ElementRate = box.ElementRate;
                                    }

                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {fairy.Item.Name} ({fairy.ElementRate}%)", 12));
                                        session.SendPacket(session.Character.GenerateInventoryAdd(fairy.ItemVNum, newInv.Amount, fairy.Type, newInv.Slot, 0, 0, fairy.Upgrade, 0));
                                        session.Character.Inventory.RemoveItemAmountFromInventory(1, box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }
                    if (EffectValue == 4)
                    {
                        BoxInstance box = session.Character.Inventory.LoadBySlotAndType<BoxInstance>(inv.Slot, InventoryType.Equipment);
                        if (box != null)
                        {
                            if (box.HoldingVNum == 0)
                            {
                                session.SendPacket($"guri 24 0 {inv.Slot}");
                            }
                            else
                            {
                                ItemInstance newInv = session.Character.Inventory.AddNewToInventory(box.HoldingVNum);
                                if (newInv != null)
                                {
                                    short Slot = inv.Slot;
                                    if (Slot != -1)
                                    {
                                        session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {newInv.Item.Name} x 1)", 12));
                                        session.SendPacket(session.Character.GenerateInventoryAdd(newInv.ItemVNum, newInv.Amount, newInv.Type, newInv.Slot, 0, 0, newInv.Upgrade, 0));
                                        session.Character.Inventory.RemoveItemAmountFromInventory(1, box.Id);
                                    }
                                }
                                else
                                {
                                    session.SendPacket(session.Character.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                }
                            }
                        }
                    }

                    break;

                default:
                    Logger.Log.Warn(string.Format(Language.Instance.GetMessageFromKey("NO_HANDLER_ITEM"), GetType()));
                    break;
            }
        }

        #endregion
    }
}