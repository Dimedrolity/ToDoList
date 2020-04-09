﻿﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ToDoList
{
    public class ToDoList : IToDoList
    {
        private readonly Dictionary<int, HashSet<AddAction>> _entryIdToAddActions =
            new Dictionary<int, HashSet<AddAction>>();

        private readonly Dictionary<int, HashSet<RemoveAction>> _entryIdToRemoveActions =
            new Dictionary<int, HashSet<RemoveAction>>();

        private readonly Dictionary<int, HashSet<StateChangeAction>> _entryIdToStateChangeActions =
            new Dictionary<int, HashSet<StateChangeAction>>();

        private readonly HashSet<int> _dismissedUsersIds = new HashSet<int>();

        public void AddEntry(int entryId, int userId, string name, long timestamp)
        {
            if (!_entryIdToAddActions.ContainsKey(entryId))
                _entryIdToAddActions[entryId] = new HashSet<AddAction>();

            _entryIdToAddActions[entryId].Add(new AddAction(timestamp, userId, name));
        }

        public void RemoveEntry(int entryId, int userId, long timestamp)
        {
            if (!_entryIdToRemoveActions.ContainsKey(entryId))
                _entryIdToRemoveActions[entryId] = new HashSet<RemoveAction>();

            _entryIdToRemoveActions[entryId].Add(new RemoveAction(timestamp, userId));
        }

        public void MarkDone(int entryId, int userId, long timestamp)
        {
            if (!_entryIdToStateChangeActions.ContainsKey(entryId))
                _entryIdToStateChangeActions[entryId] = new HashSet<StateChangeAction>();

            _entryIdToStateChangeActions[entryId].Add(new StateChangeAction(timestamp, userId, EntryState.Done));
        }

        public void MarkUndone(int entryId, int userId, long timestamp)
        {
            if (!_entryIdToStateChangeActions.ContainsKey(entryId))
                _entryIdToStateChangeActions[entryId] = new HashSet<StateChangeAction>();

            _entryIdToStateChangeActions[entryId].Add(new StateChangeAction(timestamp, userId, EntryState.Undone));
        }

        public void DismissUser(int userId) => _dismissedUsersIds.Add(userId);

        public void AllowUser(int userId) => _dismissedUsersIds.Remove(userId);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<Entry> GetEnumerator() => GetConsistentEntries().GetEnumerator();

        public int Count => GetConsistentEntries().Count();

        private IEnumerable<Entry> GetConsistentEntries()
        {
            var entryIdToAddActionsFromAllowedUsers =
                GetAllowedUsersActionsFrom(_entryIdToAddActions);

            var entryIdToRemoveActionsFromAllowedUsers =
                GetAllowedUsersActionsFrom(_entryIdToRemoveActions);

            var entryIdToStateChangeActionsFromAllowedUsers =
                GetAllowedUsersActionsFrom(_entryIdToStateChangeActions);

            var merger = new ActionsMerger();
            var consistentEntries = merger.MergeActionsToConsistentEntries(entryIdToAddActionsFromAllowedUsers,
                entryIdToRemoveActionsFromAllowedUsers, entryIdToStateChangeActionsFromAllowedUsers);

            return consistentEntries;

            
            Dictionary<int, HashSet<TAction>> GetAllowedUsersActionsFrom<TAction>(
                Dictionary<int, HashSet<TAction>> entryIdToActions) where TAction : UserAction
            {
                bool IsActionFromAllowedUser(TAction action) => !_dismissedUsersIds.Contains(action.UserId);
                
                return entryIdToActions
                    .Where(pair => pair.Value.Any(IsActionFromAllowedUser))
                    .ToDictionary(pair => pair.Key, pair => pair.Value.Where(IsActionFromAllowedUser).ToHashSet());
            }
        }
    }

    public abstract class UserAction : IComparable<UserAction>
    {
        protected UserAction(long timestamp, int userId)
        {
            Timestamp = timestamp;
            UserId = userId;
        }

        public long Timestamp { get; }
        public int UserId { get; }

        public int CompareTo(UserAction other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Timestamp.CompareTo(other.Timestamp);
        }
    }

    public class AddAction : UserAction, IComparable<AddAction>
    {
        public AddAction(long timestamp, int userId, string entryName) : base(timestamp, userId)
        {
            EntryName = entryName;
        }

        public string EntryName { get; }

        public int CompareTo(AddAction other)
        {
            var timestampComparison = base.CompareTo(other);
            return timestampComparison != 0 ? timestampComparison : UserId.CompareTo(other.UserId) * -1;
        }
    }

    public class RemoveAction : UserAction, IComparable<RemoveAction>
    {
        public RemoveAction(long timestamp, int userId) : base(timestamp, userId)
        {
        }

        public int CompareTo(RemoveAction other)
        {
            var timestampComparison = base.CompareTo(other);
            return timestampComparison;
        }
    }

    public class StateChangeAction : UserAction, IComparable<StateChangeAction>
    {
        public StateChangeAction(long timestamp, int userId, EntryState state) : base(timestamp, userId)
        {
            State = state;
        }

        public EntryState State { get; }

        public int CompareTo(StateChangeAction other)
        {
            var timestampComparison = base.CompareTo(other);
            if (timestampComparison != 0) return timestampComparison;

            if (State == other.State) return 0;
            if (State == EntryState.Undone) return 1;
            return -1;
        }
    }

    public class ActionsMerger
    {
        public IEnumerable<Entry> MergeActionsToConsistentEntries(
            Dictionary<int, HashSet<AddAction>> entryIdToAddActions,
            Dictionary<int, HashSet<RemoveAction>> entryIdToRemoveActions,
            Dictionary<int, HashSet<StateChangeAction>> entryIdToStateChangeActions)
        {
            var entryIdToFilteredAddActions =
                FilterAddActionsByRemoveActions(entryIdToAddActions, entryIdToRemoveActions);

            var entriesWithDifferentIdsAndActualNames =
                CreateOneEntryWithActualNameForEachEntryIdFrom(entryIdToFilteredAddActions);

            var consistentEntries = UpdateStatesOfEntries(entriesWithDifferentIdsAndActualNames,
                entryIdToStateChangeActions);

            return consistentEntries;
        }

        private static Dictionary<int, HashSet<AddAction>>
            FilterAddActionsByRemoveActions(
                Dictionary<int, HashSet<AddAction>> entryIdToAddActions,
                Dictionary<int, HashSet<RemoveAction>> entryIdToRemoveActions)
        {
            var filteredEntryIdToAddActions = new Dictionary<int, HashSet<AddAction>>();

            foreach (var (entryId, addActions) in entryIdToAddActions)
            {
                if (!entryIdToRemoveActions.ContainsKey(entryId))
                {
                    filteredEntryIdToAddActions.Add(entryId, addActions);
                    continue;
                }

                var timestampOfLastAddAction = addActions.Max().Timestamp;
                var timestampOfLastRemoveAction = entryIdToRemoveActions[entryId].Max().Timestamp;

                if (timestampOfLastAddAction > timestampOfLastRemoveAction)
                    filteredEntryIdToAddActions.Add(entryId, addActions);
            }

            return filteredEntryIdToAddActions;
        }
        
        private IEnumerable<Entry> CreateOneEntryWithActualNameForEachEntryIdFrom(
            Dictionary<int, HashSet<AddAction>> entryIdToAddActions)
        {
            var entriesWithDifferentIdsAndActualNames = new HashSet<Entry>();

            foreach (var (entryId, addActions) in entryIdToAddActions)
            {
                var lastAddAction = addActions.Max();
                var entryWithActualName = Entry.Undone(entryId, lastAddAction.EntryName);
                entriesWithDifferentIdsAndActualNames.Add(entryWithActualName);
            }

            return entriesWithDifferentIdsAndActualNames;
        }

        private static IEnumerable<Entry> UpdateStatesOfEntries(IEnumerable<Entry> entriesWithDifferentIds,
            Dictionary<int, HashSet<StateChangeAction>> entryIdToStateChangeActions)
        {
            var entriesWithUpdatedStates = new HashSet<Entry>();

            foreach (var entry in entriesWithDifferentIds)
            {
                if (!entryIdToStateChangeActions.ContainsKey(entry.Id))
                {
                    entriesWithUpdatedStates.Add(entry);
                    continue;
                }

                var lastStateChangeAction = entryIdToStateChangeActions[entry.Id].Max();

                var updatedEntry = lastStateChangeAction.State == EntryState.Undone
                    ? entry.MarkUndone()
                    : entry.MarkDone();

                entriesWithUpdatedStates.Add(updatedEntry);
            }

            return entriesWithUpdatedStates;
        }
    }

    // Dictionary уже имеет Deconstruct, но начиная с .Net Core 2.0;
    // Вставил сюда реализацию этого метода расширения, чтобы повысить читаемость кода.
    public static class Extensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> keyValuePair, out T1 key, out T2 value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }
    }
}