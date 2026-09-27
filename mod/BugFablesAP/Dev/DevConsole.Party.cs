using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    internal static partial class DevConsole
    {
        private static string Spawn(string[] parts)
        {
            if (parts.Length < 3)
            {
                return "spawn <item|key|medal> <id> [flag] | spawn member <n> [x z]";
            }
            if (parts[1].ToLowerInvariant() == "member")
            {
                return SpawnMember(parts);
            }
            int kind = parts[1].ToLowerInvariant() == "medal" ? 2 : parts[1].ToLowerInvariant() == "key" ? 1 : 0;
            int id = int.Parse(parts[2]);
            int flag = parts.Length > 3 ? int.Parse(parts[3]) : -1;
            if (MainManager.player == null || MainManager.map == null)
            {
                return "not now: no player";
            }
            Vector3 at = MainManager.player.transform.position + new Vector3(1.5f, 1f, 0f);
            NPCControl item = EntityControl.CreateItem(at, kind, id, Vector3.zero, -1);
            item.activationflag = flag;
            if (flag >= 0)
            {
                MainManager.instance.flags[flag] = false;
            }
            return $"spawned {parts[1]} {id}" + (flag >= 0 ? $" with flag {flag}" : "") + " next to you";
        }

        // The game redraws a pickup's own sprite, so the look is put back, as ItemSwap.TickGround does for locations.
        private static readonly List<KeyValuePair<NPCControl, Sprite>> memberLooks = new List<KeyValuePair<NPCControl, Sprite>>();

        private static void KeepMemberLooks()
        {
            if (memberLooks.Count == 0 || Time.frameCount % 15 != 0)
            {
                return;
            }
            memberLooks.RemoveAll(look => look.Key == null);
            foreach (KeyValuePair<NPCControl, Sprite> look in memberLooks)
            {
                SpriteRenderer shown = look.Key.entity != null ? look.Key.entity.sprite : null;
                if (shown != null && shown.sprite != look.Value)
                {
                    shown.sprite = look.Value;
                    if (look.Key.entity.spritetransform != null)
                    {
                        look.Key.entity.spritetransform.localPosition = new Vector2(0f, look.Value.bounds.extents.y);
                    }
                }
            }
        }

        // Looks only: a Crunchy Leaf pickup drawn as party member n, as a location holding him is; taking it gives the leaf.
        private static string SpawnMember(string[] parts)
        {
            if (MainManager.player == null || MainManager.map == null)
            {
                return "not now: no player";
            }
            int member = int.Parse(parts[2]);
            float x = parts.Length > 4 ? float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : 1.5f;
            float z = parts.Length > 4 ? float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture) : 0f;
            Vector3 at = MainManager.player.transform.position + new Vector3(x, 1f, z);
            NPCControl item = EntityControl.CreateItem(at, 0, 0, Vector3.zero, -1);
            ItemSwap.DescribeOurs(ItemIds.Base + ItemIds.MemberOffset + member, ItemIds.MemberKind, out string name, out Sprite sprite, out _);
            if (sprite != null)
            {
                memberLooks.Add(new KeyValuePair<NPCControl, Sprite>(item, sprite));
                if (item.entity != null && item.entity.sprite != null)
                {
                    item.entity.sprite.sprite = sprite;
                }
            }
            return $"spawned {name}'s look at {x}, {z} from you (a Crunchy Leaf if taken)";
        }

        private static string Nudge(string[] parts)
        {
            if (parts.Length < 3 || MainManager.player == null)
            {
                return "nudge <x> <y> <z>";
            }
            float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            float z = parts.Length > 3 ? float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : 0f;
            MainManager.player.transform.position += new Vector3(x, y, z);
            MainManager.TeleportFollowers(true);
            return "moved to " + MainManager.player.transform.position;
        }

        // ChangeParty needs fromscratch (else its copy loop never runs and the party list comes out empty); then
        // SetPlayers makes the characters. Memory only until the game saves.
        private static string AddLeif()
        {
            MainManager mm = MainManager.instance;
            if (MainManager.player == null || mm.inevent || mm.message || MainManager.battle != null)
            {
                return "addleif: not now (no player, or an event, dialogue or battle)";
            }
            if (mm.playerdata.Any(p => p.trueid == 2))
            {
                return "addleif: Leif is already in the party";
            }
            Vector3 at = MainManager.player.transform.position;
            MainManager.ChangeParty(new[] { 0, 1, 2 }, true, true);
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            return "addleif: party now " + string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())
                + $"; characters {mm.playerdata.Count(p => p.entity != null)} of {mm.playerdata.Length}";
        }
    }
}
