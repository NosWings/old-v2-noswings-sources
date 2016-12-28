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
            public byte amount;
            public short itemvnum;
            public float drop_chance; // <= 100.00

            public S_RaidBoxItem(byte amount, short itemvnum, float drop_chance)
            {
                this.amount = amount;
                this.itemvnum = itemvnum;
                this.drop_chance = drop_chance;
            }
        }

        private S_RaidBoxItem gen_random_item(List<S_RaidBoxItem> items)
        {
            List<S_RaidBoxItem> tmp = new List<S_RaidBoxItem>();

            foreach (S_RaidBoxItem item in items)
                for (int i = 0; i < item.drop_chance; i++)
                    tmp.Add(item);

            return tmp[new Random().Next(tmp.Count)];
        }

        List<S_RaidBoxItem> cuby_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 265, 10),      // Doigt rouge
            new S_RaidBoxItem(1, 262, 10),      // Elvin
            new S_RaidBoxItem(1, 268, 10),      // Sage rouge
            new S_RaidBoxItem(1, 321, 15),      // Chaussure d'éclat
            new S_RaidBoxItem(1, 315, 15),      // Gant de flammes
            new S_RaidBoxItem(3, 2282, 20),     // Plume d'ange
            new S_RaidBoxItem(3, 1030, 20)      // Pleine lune
        };
        
        List<S_RaidBoxItem> xyxy_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 289, 9),       // Scrammer
            new S_RaidBoxItem(1, 293, 9),       // Arme de charme plaz
            new S_RaidBoxItem(1, 291, 9),       // Arbalète Winslet
            new S_RaidBoxItem(1, 271, 9),       // Sage bleu
            new S_RaidBoxItem(1, 295, 9),       // Regarde du gardien
            new S_RaidBoxItem(1, 297, 9),       // Défenseur brave
            new S_RaidBoxItem(4, 2282, 23),     // Plume d'ange
            new S_RaidBoxItem(4, 1030, 23)      // Pleine lune
        };

        List<S_RaidBoxItem> castra_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 266, 10),      // Paix verte
            new S_RaidBoxItem(1, 269, 10),      // Chuchotement du fântome
            new S_RaidBoxItem(1, 263, 10),      // Glorieux
            new S_RaidBoxItem(1, 318, 15),      // Gant de mort
            new S_RaidBoxItem(1, 320, 15),      // Bottes de vague
            new S_RaidBoxItem(5, 2282, 20),     // Plume d'ange
            new S_RaidBoxItem(5, 1030, 20)      // Pleine lune
        };

        List<S_RaidBoxItem> jack_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 290, 6),       // Kriss
            new S_RaidBoxItem(1, 292, 6),       // Arbalète Balenty
            new S_RaidBoxItem(1, 294, 6),       // Arme de charme rai
            new S_RaidBoxItem(1, 272, 6),       // Robe D'essai
            new S_RaidBoxItem(1, 296, 6),       // Marche de brise
            new S_RaidBoxItem(1, 298, 6),       // Défenseur splendide
            new S_RaidBoxItem(1, 319, 13),      // Botte de feu
            new S_RaidBoxItem(1, 316, 13),      // Gants de tempête
            new S_RaidBoxItem(6, 2282, 19),     // Plume d'ange
            new S_RaidBoxItem(6, 1030, 19)      // Pleine lune
        };

        List<S_RaidBoxItem> slade_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 270, 10),      // Majestueux
            new S_RaidBoxItem(1, 264, 10),      // Main
            new S_RaidBoxItem(1, 267, 10),      // Arc majestueux
            new S_RaidBoxItem(1, 317, 15),      // Gant divin
            new S_RaidBoxItem(1, 322, 15),      // Chaussure de l'obscurité
            new S_RaidBoxItem(7, 2282, 20),     // Plume d'ange
            new S_RaidBoxItem(7, 1030, 20)      // Pleine lune
        };

        List<S_RaidBoxItem> ibra_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(5, 1872, 5),      // Pièces d'or
            new S_RaidBoxItem(15, 1873, 15),    // Pièces d'argent
            new S_RaidBoxItem(30, 1874, 30),    // Pièces de cuivre
            new S_RaidBoxItem(7, 2282, 35),     // Plume d'ange
            new S_RaidBoxItem(7, 1030, 35)      // Pleine lune
        };

        List<S_RaidBoxItem> kertos_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(50, 2900, 30),     // Diamants brisés
            new S_RaidBoxItem(25, 2901, 20),     // Diamants Intacts
            new S_RaidBoxItem(15, 2505, 10),     // Cristal terrestre
            new S_RaidBoxItem(1, 4912, 6),       // Dague 92
            new S_RaidBoxItem(1, 4909, 6),       // Arba 92
            new S_RaidBoxItem(1, 4915, 6),       // Pisto 92
            new S_RaidBoxItem(1, 4917, 7),       // Pistolet du héros oublié (Fake)
            new S_RaidBoxItem(1, 4911, 7),       // Arbalète du héros oublié (Fake)
            new S_RaidBoxItem(1, 4914, 7),       // Dague du héros oublié (Fake)
            new S_RaidBoxItem(1, 4934, 1)        // Bottes de kertos
        };

        List<S_RaidBoxItem> valakus_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(50, 2900, 30),     // Diamants brisés
            new S_RaidBoxItem(25, 2901, 20),     // Diamants Intacts
            new S_RaidBoxItem(15, 2505, 10),     // Cristal terrestre
            new S_RaidBoxItem(1, 4924, 6),       // Tunique du phoenix flamboyant
            new S_RaidBoxItem(1, 4918, 6),       // Armure à plaques du géant de flammes
            new S_RaidBoxItem(1, 4921, 6),       // Armure en cuir du géant de braise
            new S_RaidBoxItem(1, 4926, 7),       // Robe du héros oublié (Fake)
            new S_RaidBoxItem(1, 4920, 7),       // Armure du héros oublié (Fake)
            new S_RaidBoxItem(1, 4923, 7),       // Tunique du héros oublié (Fake)
            new S_RaidBoxItem(1, 4932, 1)        // Gant de valakus
        };

        List<S_RaidBoxItem> grenigas_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(50, 2900, 30),     // Diamants brisés
            new S_RaidBoxItem(25, 2901, 20),     // Diamants Intacts
            new S_RaidBoxItem(15, 2505, 10),     // Cristal terrestre
            new S_RaidBoxItem(1, 4900, 6),       // Epée incendiaire de Magmaros
            new S_RaidBoxItem(1, 4903, 6),       // Aile de phoenix
            new S_RaidBoxItem(1, 4906, 6),       // Spectre de lave
            new S_RaidBoxItem(1, 4908, 7),       // Baguette du héros oublié (Fake)
            new S_RaidBoxItem(1, 4902, 7),       // Epée du héros oublié (Fake)
            new S_RaidBoxItem(1, 4905, 7),       // Arc du héros oublié (Fake)
            new S_RaidBoxItem(1, 4930, 1)        // Casque des flammes
        };

        List<S_RaidBoxItem> draco_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 2514, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2515, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2516, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2517, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2518, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2519, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2520, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2521, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 4500, 5),       // SP5
            new S_RaidBoxItem(1, 4501, 5),       // SP5
            new S_RaidBoxItem(1, 4502, 5),       // SP5
            new S_RaidBoxItem(2, 2349, 20),      // Pierre brillante bleu ciel
            new S_RaidBoxItem(3, 2349, 20),      // Pierre brillante bleu ciel
            new S_RaidBoxItem(10, 2282, 18),     // Plume d'ange
            new S_RaidBoxItem(5, 1030, 19)       // Pleine lune
        };

        List<S_RaidBoxItem> glagla_loots = new List<S_RaidBoxItem>
        {
            new S_RaidBoxItem(1, 2514, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2515, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2516, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2517, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2518, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2519, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2520, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 2521, 1),       // Pierre de SP
            new S_RaidBoxItem(1, 4497, 5),       // SP6
            new S_RaidBoxItem(1, 4498, 5),       // SP6
            new S_RaidBoxItem(1, 4499, 5),       // SP6
            new S_RaidBoxItem(2, 2349, 20),      // Pierre brillante bleu ciel
            new S_RaidBoxItem(3, 2349, 20),      // Pierre brillante bleu ciel
            new S_RaidBoxItem(10, 2282, 18),     // Plume d'ange
            new S_RaidBoxItem(5, 1030, 19)       // Pleine lune
        };

        public override void Use(ClientSession session, ref ItemInstance inv, bool delay = false, string[] packetsplit = null)
        {
            S_RaidBoxItem rand_item;
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
                                rand_item = gen_random_item(xyxy_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 2: //CASTRA
                                rand_item = gen_random_item(castra_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 3: //JACK
                                rand_item = gen_random_item(jack_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 4: //SLADE
                                rand_item = gen_random_item(slade_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 9: //IBRAHIM
                                rand_item = gen_random_item(ibra_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 13: //KERTOS
                                rand_item = gen_random_item(kertos_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 14: //VALAKUS
                                rand_item = gen_random_item(valakus_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 15: //GRENIGAS
                                rand_item = gen_random_item(grenigas_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 16: //DRACO
                                rand_item = gen_random_item(draco_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            case 17: //GLAGLA
                                rand_item = gen_random_item(glagla_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                            default: //CUBY
                                rand_item = gen_random_item(cuby_loots);
                                numberOfItem = rand_item.amount;
                                newInv = session.Character.Inventory.AddNewToInventory(rand_item.itemvnum, rand_item.amount);
                                break;
                        }
                        if (newInv != null)
                        {
                            dynamic raidBoxItem;
                            if (newInv.Type == InventoryType.Equipment)
                            {
                                if (newInv.Item.EquipmentSlot == EquipmentType.Armor ||
                                    newInv.Item.EquipmentSlot == EquipmentType.MainWeapon ||
                                    newInv.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
                                {
                                    newInv.Rare = raidBox.Rare;
                                }
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

                            short slot = inv.Slot;
                            if (slot != -1)
                            {
                                session.SendPacket($"rdi {raidBoxItem.Item.VNum} {numberOfItem}");
                                session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")}: {raidBoxItem.Item.Name} x {rand_item.amount}", 12));
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