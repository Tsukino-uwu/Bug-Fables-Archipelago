using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    internal sealed partial class ApMenu
    {
        private void TypeInto()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.V))
            {
                string pasted = Clean(GUIUtility.systemCopyBuffer);
                if (row == PortRow)
                {
                    pasted = new string(Array.FindAll(pasted.ToCharArray(), char.IsDigit));
                }
                else if (row == Address)
                {
                    // A room page shows "archipelago.gg:63560": paste it whole and the port goes to its own row.
                    var m = System.Text.RegularExpressions.Regex.Match(pasted, @"^(.+):(\d{1,5})$");
                    if (m.Success)
                    {
                        pasted = m.Groups[1].Value;
                        port.Value = m.Groups[2].Value;
                    }
                }
                edited += pasted;
                Redraw();
                return;
            }
            if (ctrl && Input.GetKeyDown(KeyCode.C))
            {
                GUIUtility.systemCopyBuffer = edited;
                return;
            }
            // The gamepad's confirm/cancel end editing: joykeys are raw buttons, [0] confirm and [1] cancel ([4]/[5] are
            // Start and Back). Only the pad is read, so the keyboard's C and X still type.
            bool pad = MainManager.usejoystick > 0;
            bool padConfirm = pad && InputIOManager.InputIO.GetKeyDown(0, true);
            bool padCancel = pad && InputIOManager.InputIO.GetKeyDown(1, true);
            if (Input.GetKeyDown(KeyCode.Escape) || padCancel)
            {
                edited = before;
                FinishEdit(keep: false);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || padConfirm)
            {
                FinishEdit(keep: true);
                return;
            }
            bool changed = false;
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    if (edited.Length > 0)
                    {
                        edited = edited.Substring(0, edited.Length - 1);
                        changed = true;
                    }
                }
                else if (c >= ' ' && !ctrl && edited.Length < 64 && (row != PortRow || char.IsDigit(c)))
                {
                    edited += c;
                    changed = true;
                }
            }
            if (changed)
            {
                Redraw();
            }
        }

        private void FinishEdit(bool keep)
        {
            if (keep)
            {
                Field(row).Value = edited.Trim();
            }
            editing = false;
            settleFrames = 2;
            MainManager.PlaySound(keep ? "Confirm" : "Cancel", -1);
            Redraw();
        }

        private ConfigEntry<string> Field(int r) => r == Address ? server : r == PortRow ? port : r == SlotRow ? slot : password;

        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (c >= ' ' && c != '|')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Trim();
        }

        // The game's text commands are written between '|', so a '|' typed into a field would be read as one.
        private static string Safe(string s) => (s ?? "").Replace("|", "");
    }
}
