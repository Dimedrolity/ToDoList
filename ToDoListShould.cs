﻿using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace ToDoList
{
    public class ToDoListShould
    {
        private const int UserA = 10;
        private const int UserB = 11;
        private const int UserC = 14;

        private IToDoList _list;

        [SetUp]
        public void SetUp()
        {
            _list = new ToDoList();
        }

        [Test]
        public void Add_Entry()
        {
            _list.AddEntry(42, UserA, "Build project", 100);

            AssertEntries(Entry.Undone(42, "Build project"));
        }

        [Test]
        public void Not_Remove_Entry_If_Removal_Timestamp_Is_Less_Than_Entry_Timestamp(
            [Values(UserA, UserB, UserC)] int removingUserId)
        {
            _list.AddEntry(42, UserA, "Build project", 100);

            _list.RemoveEntry(42, removingUserId, 99);

            AssertEntries(Entry.Undone(42, "Build project"));
        }

        [Test]
        public void Remove_Entry(
            [Values(UserA, UserB, UserC)] int removingUserId,
            [Values(200, 101, 100)] long removingTimestamp)
        {
            _list.AddEntry(42, UserA, "Build project", 100);

            _list.RemoveEntry(42, removingUserId, removingTimestamp);

            AssertListEmpty();
        }

        [Test]
        public void Updates_Name_When_Entry_With_Greater_Timestamp_Added(
            [Values(UserA, UserB, UserC)] int updatingUserId)
        {
            _list.AddEntry(42, UserB, "Build project", 100);

            _list.AddEntry(42, updatingUserId, "Create project", 105);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Not_Update_Name_When_Less_Experienced_User_Adds_Entry()
        {
            _list.AddEntry(42, UserB, "Create project", 100);

            _list.AddEntry(42, UserA, "Build project", 100);

            AssertEntries(Entry.Undone(42, "Build project"));
        }

        [Test]
        public void Update_Name_When_More_Experienced_User_Adds_Entry()
        {
            _list.AddEntry(42, UserA, "Create project", 100);

            _list.AddEntry(42, UserB, "Build project", 100);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Add_Several_Entries()
        {
            _list.AddEntry(42, UserA, "Create audio subsystem", 150);
            _list.AddEntry(90, UserB, "Create video subsystem", 125);
            _list.AddEntry(74, UserC, "Create input subsystem", 117);

            AssertEntries(
                Entry.Undone(42, "Create audio subsystem"),
                Entry.Undone(90, "Create video subsystem"),
                Entry.Undone(74, "Create input subsystem")
            );
        }

        [Test]
        public void AddEntry_Remove_Add()
        {
            _list.AddEntry(42, UserA, "Create audio subsystem", 100);
            _list.RemoveEntry(42, UserA, 101);
            _list.AddEntry(42, UserA, "Create audio subsystem", 102);


            AssertEntries(Entry.Undone(42, "Create audio subsystem"));
        }

        [Test]
        public void Mark_Entry_Done(
            [Values(UserA, UserB, UserC)] int markingUserId,
            [Values(100, 95, 107)] long markTimestamp)
        {
            _list.AddEntry(42, UserB, "Create project", 100);

            _list.MarkDone(42, markingUserId, markTimestamp);

            AssertEntries(Entry.Done(42, "Create project"));
        }

        [Test]
        public void Mark_Done_And_Remove_Entry(
            [Values(10)] int markingUserId,
            [Values(101)] long markTimestamp)
        {
            _list.AddEntry(42, 10, "Create project", 100);

            _list.MarkDone(42, markingUserId, markTimestamp);

            _list.RemoveEntry(42, 10, 102);

            AssertListEmpty();
        }

        [Test]
        public void Remove_Entry_And_Mark_Done_And_Add()
        {
            _list.AddEntry(42, 10, "Create project", 100);

            _list.RemoveEntry(42, 10, 101);

            _list.MarkDone(42, 10, 102);
                        
            _list.AddEntry(42, 10, "Create project - 103", 103);

            AssertEntries(Entry.Done(42, "Create project - 103"));
        }

        [Test]
        public void Mark_Entry_Done_When_Entry_Does_Not_Exists(
            [Values(UserA, UserB, UserC)] int markingUserId,
            [Values(100, 95, 107)] long markTimestamp)
        {
            _list.MarkDone(42, markingUserId, markTimestamp);

            _list.AddEntry(42, UserB, "Create project", 100);

            AssertEntries(Entry.Done(42, "Create project"));
        }

        [Test]
        public void Mark_Undone()
        {
            _list.AddEntry(42, UserA, "Create project", 100);
            _list.MarkDone(42, UserB, 105);

            _list.MarkUndone(42, UserC, 106);

            AssertEntries(Entry.Undone(42, "Create project"));
        }


        [Test]
        public void AddAndRemove_DontAffectOnState()
        {
            _list.AddEntry(42, UserA, "OldName", 100);
            _list.RemoveEntry(42, UserA, 105);
            _list.MarkDone(42, UserB, 5); //любой timestamp
            _list.AddEntry(42, UserA, "NewName", 110);

            AssertEntries(Entry.Done(42, "NewName"));
        }


        [Test]
        public void DismissUserAndGetEmptyList_FromChat()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);

            _list.DismissUser(UserA); // вот  тут ты убираешь все то, что прислал userA

            AssertListEmpty();
        }

        [Test]
        public void DismissUserAndAddEntryFromAnotherUser_FromChat()
        {
            _list.AddEntry(10, UserA, "abc", 100);
            _list.MarkDone(10, UserB, 105);
            _list.DismissUser(UserA);
            _list.AddEntry(10, UserC, "cba", 110);

            AssertEntries(Entry.Done(10, "cba"));
        }

        [Test]
        public void Not_Mark_Undone_When_Timestamp_Less_Than_Done_Mark_Timestamp2()
        {
            _list.AddEntry(42, UserA, "Create project", 100);
            _list.MarkDone(42, UserB, 105);

            _list.MarkUndone(42, UserC, 107);
            _list.MarkUndone(42, UserC, 99);

            AssertEntries(Entry.Undone(42, "Create project"));
        }

        [Test]
        public void Dismiss_User_That_Did_Nothing()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);

            _list.DismissUser(UserC);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Dismiss_Creation()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);

            _list.DismissUser(UserA);

            AssertListEmpty();
        }

        [Test]
        public void Dismiss_Name_Updates()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.AddEntry(42, UserB, "Introduce nice autotests", 105);

            _list.DismissUser(UserB);

            AssertEntries(Entry.Undone(42, "Introduce autotests"));
        }

        [Test]
        public void Dismiss_Done()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);

            _list.DismissUser(UserB);

            AssertEntries(Entry.Undone(42, "Introduce autotests"));
        }

        [Test]
        public void Dismiss_Undone()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserA, 105);
            _list.MarkUndone(42, UserB, 107);

            _list.DismissUser(UserB);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_User_That_Did_Nothing()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);
            _list.DismissUser(UserC);

            _list.AllowUser(UserC);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_Creation()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);
            _list.DismissUser(UserA);

            _list.AllowUser(UserA);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_Name_Updates()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.AddEntry(42, UserB, "Introduce nice autotests", 105);
            _list.DismissUser(UserB);

            _list.AllowUser(UserB);

            AssertEntries(Entry.Undone(42, "Introduce nice autotests"));
        }

        [Test]
        public void Allow_Done()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserB, 105);
            _list.DismissUser(UserB);

            _list.AllowUser(UserB);

            AssertEntries(Entry.Done(42, "Introduce autotests"));
        }

        [Test]
        public void Allow_Undone()
        {
            _list.AddEntry(42, UserA, "Introduce autotests", 100);
            _list.MarkDone(42, UserA, 105);
            _list.MarkUndone(42, UserB, 107);
            _list.DismissUser(UserB);

            _list.AllowUser(UserB);

            AssertEntries(Entry.Undone(42, "Introduce autotests"));
        }

        private void AssertListEmpty()
        {
            _list.Should().BeEmpty();
            _list.Count.Should().Be(0);
        }

        private void AssertEntries(params Entry[] expected)
        {
            _list.Should().BeEquivalentTo(expected.AsEnumerable());
            _list.Count.Should().Be(expected.Length);
        }
    }
}