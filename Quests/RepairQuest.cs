using System;
using DooDesch.Localization;
using S1API.Quests;

namespace RVRepairVan.Quests
{
    /// <summary>
    /// Drives the "Back on the Road" quest lifecycle. Start is triggered by the explosion-beat
    /// auto-start in Questline.ProximityCoroutine; completion is triggered by reaching the
    /// repaired RV. S1API persists the active quest per save, so we look it up by name rather
    /// than holding a stale reference across reloads.
    /// </summary>
    internal static class RepairQuest
    {
        // Localized once at first use; every lookup (GetQuestByName) goes through this same field,
        // so create/find/complete stay consistent within a session regardless of language.
        internal static readonly string Title = L10n.T("Back on the Road");

        internal static bool IsActive()
        {
            try
            {
                return QuestManager.GetQuestByName(Title) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// True once the quest exists AND its entry is populated. The entry is added in OnCreated, which S1API
        /// fires on the game's Quest.Start event a frame or two AFTER CreateQuest - so right after StartIfNeeded
        /// the quest exists but has no entry yet, and UpdateEntry / SyncEntry would no-op.
        /// </summary>
        internal static bool HasEntry()
        {
            try
            {
                Quest quest = QuestManager.GetQuestByName(Title);
                return quest?.QuestEntries != null && quest.QuestEntries.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        internal static void StartIfNeeded()
        {
            try
            {
                if (IsActive())
                {
                    Core.Log.Msg("[Quest] '" + Title + "' already active.");
                    return;
                }

                QuestManager.CreateQuest<RepairRVQuest>();
                Core.Log.Msg("[Quest] '" + Title + "' started.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] start failed: " + e.Message);
            }
        }

        /// <summary>Retitle + re-point the (single) quest entry - used by the questline per stage.</summary>
        internal static void UpdateEntry(string title, Vector3 poi)
        {
            try
            {
                Quest quest = QuestManager.GetQuestByName(Title);
                if (quest?.QuestEntries != null && quest.QuestEntries.Count > 0)
                {
                    QuestEntry entry = quest.QuestEntries[0];
                    entry.Title = title;
                    entry.POIPosition = poi;
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] update entry failed: " + e.Message);
            }
        }

        /// <summary>
        /// Retitle + point the entry at an NPC. Binds the marker to the NPC's live transform (S1API
        /// SetPOIToNPC) so it tracks Donna/Ming/Marco as they walk their schedule, instead of freezing
        /// at wherever they happened to be standing when the stage advanced. Falls back to a fixed point
        /// if the NPC can't be resolved (e.g. not spawned yet).
        /// </summary>
        internal static void UpdateEntry(string title, NPC npc, Vector3 fallback)
        {
            try
            {
                Quest quest = QuestManager.GetQuestByName(Title);
                if (quest?.QuestEntries == null || quest.QuestEntries.Count == 0) return;

                QuestEntry entry = quest.QuestEntries[0];
                entry.Title = title;

                S1API.Entities.NPC s1npc = npc != null ? S1API.Entities.NPC.Get(npc.ID) : null;
                if (s1npc != null && entry.SetPOIToNPC(s1npc))
                    Core.LogDebug("[Quest] marker now follows NPC '" + npc.ID + "' (live).");
                else
                {
                    entry.POIPosition = fallback;
                    Core.LogDebug("[Quest] marker set to fixed point " + fallback + " (NPC follow unavailable).");
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] update entry failed: " + e.Message);
            }
        }

        internal static void CompleteIfActive()
        {
            try
            {
                Quest quest = QuestManager.GetQuestByName(Title);
                if (quest == null)
                {
                    return;
                }

                if (quest.QuestEntries != null)
                {
                    foreach (QuestEntry entry in quest.QuestEntries)
                    {
                        try { entry.Complete(); }
                        catch { /* keep completing the rest */ }
                    }
                }

                Core.Log.Msg("[Quest] '" + Title + "' completed.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] complete failed: " + e.Message);
            }
        }
    }
}
