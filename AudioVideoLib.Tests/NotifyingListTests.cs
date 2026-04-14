namespace AudioVideoLib.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using AudioVideoLib.Collections;
using Xunit;

public class NotifyingListTests
{
    ////------------------------------------------------------------------------------------------------------------------------------
    // Basic operations
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Add_IncreasesCount()
    {
        NotifyingList<int> list = [];

        list.Add(42);

        Assert.Single(list);
        Assert.Equal(42, list[0]);
    }

    [Fact]
    public void Add_MultipleItems_MaintainsOrder()
    {
        NotifyingList<int> list = [];

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Insert_AtIndex_PlacesItemCorrectly()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(3);

        list.Insert(1, 2);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Insert_AtZero_PrependsItem()
    {
        NotifyingList<int> list = [];
        list.Add(2);
        list.Add(3);

        list.Insert(0, 1);

        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void Remove_ExistingItem_ReturnsTrue()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);

        var result = list.Remove(10);

        Assert.True(result);
        Assert.Single(list);
        Assert.Equal(20, list[0]);
    }

    [Fact]
    public void Remove_NonExistentItem_ReturnsFalse()
    {
        NotifyingList<int> list = [];
        list.Add(10);

        var result = list.Remove(99);

        Assert.False(result);
        Assert.Single(list);
    }

    [Fact]
    public void RemoveAt_RemovesItemAtIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(30);

        list.RemoveAt(1);

        Assert.Equal(2, list.Count);
        Assert.Equal(10, list[0]);
        Assert.Equal(30, list[1]);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void IndexOf_ExistingItem_ReturnsCorrectIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(30);

        Assert.Equal(1, list.IndexOf(20));
    }

    [Fact]
    public void IndexOf_NonExistentItem_ReturnsNegativeOne()
    {
        NotifyingList<int> list = [];
        list.Add(10);

        Assert.Equal(-1, list.IndexOf(99));
    }

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        NotifyingList<int> list = [];
        list.Add(42);

        Assert.Contains(42, list);
    }

    [Fact]
    public void Contains_NonExistentItem_ReturnsFalse()
    {
        NotifyingList<int> list = [];
        list.Add(42);

        Assert.DoesNotContain(99, list);
    }

    [Fact]
    public void Count_EmptyList_ReturnsZero()
    {
        NotifyingList<int> list = [];

        Assert.Empty(list);
    }

    [Fact]
    public void Indexer_Get_ReturnsCorrectItem()
    {
        NotifyingList<int> list = [];
        list.Add(100);

        Assert.Equal(100, list[0]);
    }

    [Fact]
    public void Indexer_Set_ReplacesItem()
    {
        NotifyingList<int> list = [];
        list.Add(100);

        list[0] = 200;

        Assert.Equal(200, list[0]);
    }

    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        NotifyingList<int> list = [];

        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void CopyTo_CopiesElementsToArray()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);
        var array = new int[5];

        list.CopyTo(array, 1);

        Assert.Equal(0, array[0]);
        Assert.Equal(1, array[1]);
        Assert.Equal(2, array[2]);
        Assert.Equal(3, array[3]);
        Assert.Equal(0, array[4]);
    }

    [Fact]
    public void ToArray_ReturnsArrayCopy()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);

        var array = list.ToArray();

        Assert.Equal(2, array.Length);
        Assert.Equal(1, array[0]);
        Assert.Equal(2, array[1]);
    }

    [Fact]
    public void Capacity_CanBeSetAndRetrieved()
    {
        var list = new NotifyingList<int>(16);

        Assert.True(list.Capacity >= 16);

        list.Capacity = 32;

        Assert.Equal(32, list.Capacity);
    }

    [Fact]
    public void AddRange_AddsAllItems()
    {
        NotifyingList<int> list = [];

        list.AddRange([10, 20, 30]);

        Assert.Equal(3, list.Count);
        Assert.Equal(10, list[0]);
        Assert.Equal(20, list[1]);
        Assert.Equal(30, list[2]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Collection initializer
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void CollectionInitializer_CreatesPopulatedList()
    {
        NotifyingList<int> list = [1, 2, 3];

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // ItemAdd event (before add)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ItemAdd_FiresBeforeAdd_WithCorrectItemAndIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        var firedItem = 0;
        var firedIndex = -99;
        list.ItemAdd += (_, e) =>
        {
            firedItem = e.Item;
            firedIndex = e.Index;
        };

        list.Add(20);

        Assert.Equal(20, firedItem);
        // When adding to end, the index passed to the event is -1.
        Assert.Equal(-1, firedIndex);
    }

    [Fact]
    public void ItemAdd_FiresBeforeInsert_WithCorrectIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(30);
        var firedIndex = -99;
        list.ItemAdd += (_, e) =>
        {
            firedIndex = e.Index;
        };

        list.Insert(1, 20);

        Assert.Equal(1, firedIndex);
    }

    [Fact]
    public void ItemAdd_Cancel_PreventsAdd()
    {
        NotifyingList<int> list = [];
        list.ItemAdd += (_, e) =>
        {
            e.Cancel = true;
        };

        list.Add(42);

        Assert.Empty(list);
    }

    [Fact]
    public void ItemAdd_Cancel_PreventsItemAddedFromFiring()
    {
        NotifyingList<int> list = [];
        var addedFired = false;
        list.ItemAdd += (_, e) =>
        {
            e.Cancel = true;
        };
        list.ItemAdded += (_, _) =>
        {
            addedFired = true;
        };

        list.Add(42);

        Assert.False(addedFired);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // ItemAdded event (after add)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ItemAdded_FiresAfterAdd_WithCorrectItemAndIndex()
    {
        NotifyingList<int> list = [];
        var firedItem = 0;
        var firedIndex = -99;
        list.ItemAdded += (_, e) =>
        {
            firedItem = e.Item;
            firedIndex = e.Index;
        };

        list.Add(42);

        Assert.Equal(42, firedItem);
        Assert.Equal(0, firedIndex);
    }

    [Fact]
    public void ItemAdded_FiresAfterInsert_WithCorrectIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(30);
        var firedIndex = -99;
        list.ItemAdded += (_, e) =>
        {
            firedIndex = e.Index;
        };

        list.Insert(1, 20);

        Assert.Equal(1, firedIndex);
    }

    [Fact]
    public void ItemAdded_ListAlreadyContainsItem_WhenEventFires()
    {
        NotifyingList<int> list = [];
        var countDuringEvent = -1;
        list.ItemAdded += (_, _) =>
        {
            countDuringEvent = list.Count;
        };

        list.Add(42);

        Assert.Equal(1, countDuringEvent);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // ItemRemove event (before remove)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ItemRemove_FiresBeforeRemove_WithCorrectItemAndIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        var firedItem = 0;
        var firedIndex = -99;
        list.ItemRemove += (_, e) =>
        {
            firedItem = e.Item;
            firedIndex = e.Index;
        };

        list.Remove(20);

        Assert.Equal(20, firedItem);
        Assert.Equal(1, firedIndex);
    }

    [Fact]
    public void ItemRemove_FiresBeforeRemoveAt_WithCorrectItem()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        var firedItem = 0;
        list.ItemRemove += (_, e) =>
        {
            firedItem = e.Item;
        };

        list.RemoveAt(0);

        Assert.Equal(10, firedItem);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // ItemRemoved event (after remove)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ItemRemoved_FiresAfterRemove_WithCorrectItem()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        var firedItem = 0;
        var firedIndex = -99;
        list.ItemRemoved += (_, e) =>
        {
            firedItem = e.Item;
            firedIndex = e.Index;
        };

        list.Remove(10);

        Assert.Equal(10, firedItem);
        Assert.Equal(0, firedIndex);
    }

    [Fact]
    public void ItemRemoved_ListNoLongerContainsItem_WhenEventFires()
    {
        NotifyingList<int> list = [];
        list.Add(42);
        var containsDuringEvent = true;
        list.ItemRemoved += (_, _) =>
        {
            containsDuringEvent = list.Contains(42);
        };

        list.Remove(42);

        Assert.False(containsDuringEvent);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // ItemReplace event (before replace via indexer set)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ItemReplace_FiresOnIndexerSet_WithOldAndNewItem()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        var firedOld = 0;
        var firedNew = 0;
        var firedIndex = -99;
        list.ItemReplace += (_, e) =>
        {
            firedOld = e.OldItem;
            firedNew = e.NewItem;
            firedIndex = e.Index;
        };

        list[0] = 20;

        Assert.Equal(10, firedOld);
        Assert.Equal(20, firedNew);
        Assert.Equal(0, firedIndex);
    }

    [Fact]
    public void ItemReplace_Cancel_PreventsReplacement()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.ItemReplace += (_, e) =>
        {
            e.Cancel = true;
        };

        list[0] = 99;

        Assert.Equal(10, list[0]);
    }

    [Fact]
    public void ItemReplace_Cancel_PreventsItemReplacedFromFiring()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        var replacedFired = false;
        list.ItemReplace += (_, e) =>
        {
            e.Cancel = true;
        };
        list.ItemReplaced += (_, _) =>
        {
            replacedFired = true;
        };

        list[0] = 99;

        Assert.False(replacedFired);
    }

    [Fact]
    public void ItemReplaced_FiresAfterReplace_WithCorrectValues()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        var firedOld = 0;
        var firedNew = 0;
        var firedIndex = -99;
        list.ItemReplaced += (_, e) =>
        {
            firedOld = e.OldItem;
            firedNew = e.NewItem;
            firedIndex = e.Index;
        };

        list[0] = 20;

        Assert.Equal(10, firedOld);
        Assert.Equal(20, firedNew);
        Assert.Equal(0, firedIndex);
    }

    [Fact]
    public void ItemReplaced_ListContainsNewValue_WhenEventFires()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        var valueDuringEvent = 0;
        list.ItemReplaced += (_, _) =>
        {
            valueDuringEvent = list[0];
        };

        list[0] = 20;

        Assert.Equal(20, valueDuringEvent);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Cancel support on ItemRemove
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void ItemRemove_Cancel_PreventsRemoval()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.ItemRemove += (_, e) =>
        {
            e.Cancel = true;
        };

        var result = list.Remove(10);

        Assert.False(result);
        Assert.Equal(2, list.Count);
        Assert.Contains(10, list);
    }

    [Fact]
    public void ItemRemove_Cancel_PreventsItemRemovedFromFiring()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        var removedFired = false;
        list.ItemRemove += (_, e) =>
        {
            e.Cancel = true;
        };
        list.ItemRemoved += (_, _) =>
        {
            removedFired = true;
        };

        list.Remove(10);

        Assert.False(removedFired);
    }

    [Fact]
    public void ItemRemove_CancelSelectivelyByItem_OnlyPreventsMatchingRemoval()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.ItemRemove += (_, e) =>
        {
            if (e.Item == 10)
            {
                e.Cancel = true;
            }
        };

        list.Remove(10);
        list.Remove(20);

        Assert.Single(list);
        Assert.Equal(10, list[0]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // RemoveAll (predicate-based removal)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RemoveAll_RemovesMatchingItems_ReturnsCorrectCount()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);

        var removed = list.RemoveAll(x => x > 3);

        Assert.Equal(2, removed);
        Assert.Equal(3, list.Count);
        Assert.DoesNotContain(4, list);
        Assert.DoesNotContain(5, list);
    }

    [Fact]
    public void RemoveAll_NoMatches_ReturnsZero()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);

        var removed = list.RemoveAll(x => x > 100);

        Assert.Equal(0, removed);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void RemoveAll_AllMatch_RemovesAll()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var removed = list.RemoveAll(_ => true);

        Assert.Equal(3, removed);
        Assert.Empty(list);
    }

    [Fact]
    public void RemoveAll_NullPredicate_ThrowsArgumentNullException()
    {
        NotifyingList<int> list = [];

        Assert.Throws<ArgumentNullException>(() => list.RemoveAll(null!));
    }

    [Fact]
    public void RemoveAll_WithCancelledRemovals_ReturnsActuallyRemovedCount()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.ItemRemove += (_, e) =>
        {
            if (e.Item == 2)
            {
                e.Cancel = true;
            }
        };

        var removed = list.RemoveAll(_ => true);

        Assert.Equal(2, removed);
        Assert.Single(list);
        Assert.Equal(2, list[0]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // RemoveItem by index removes the exact indexed element (not first equal-by-value)
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void RemoveAt_WithDuplicates_RemovesExactIndexedElement()
    {
        // With duplicates ["same", "same", "same"], removing at index 1 should remove the second element,
        // leaving exactly 2 elements.
        NotifyingList<string> list = [];
        list.Add("same");
        list.Add("same");
        list.Add("same");

        // Track which index was reported in the remove event.
        var removedIndex = -1;
        list.ItemRemoved += (_, e) =>
        {
            removedIndex = e.Index;
        };

        list.RemoveAt(1);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, removedIndex);
    }

    [Fact]
    public void RemoveAt_WithDuplicateInts_RemovesCorrectPosition()
    {
        NotifyingList<int> list = [];
        list.Add(7);
        list.Add(7);
        list.Add(7);

        list.RemoveAt(2);

        Assert.Equal(2, list.Count);
        // Both remaining elements are still 7.
        Assert.Equal(7, list[0]);
        Assert.Equal(7, list[1]);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // FindIndex
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void FindIndex_WithPredicate_ReturnsFirstMatchIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(30);

        var index = list.FindIndex(x => x >= 20);

        Assert.Equal(1, index);
    }

    [Fact]
    public void FindIndex_NoMatch_ReturnsNegativeOne()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);

        var index = list.FindIndex(x => x > 100);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void FindIndex_WithStartIndex_SearchesFromGivenPosition()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(10);
        list.Add(30);

        var index = list.FindIndex(1, x => x == 10);

        Assert.Equal(2, index);
    }

    [Fact]
    public void FindIndex_WithStartIndexAndCount_SearchesWithinRange()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(30);
        list.Add(10);

        // Search from index 1, count 2 (covers indices 1 and 2 only).
        var index = list.FindIndex(1, 2, x => x == 10);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void FindLastIndex_ReturnsLastMatchIndex()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(10);

        var index = list.FindLastIndex(x => x == 10);

        Assert.Equal(2, index);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // AsReadOnly
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void AsReadOnly_ReturnsReadOnlyWrapper()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);

        var readOnly = list.AsReadOnly();

        Assert.Equal(2, readOnly.Count);
        Assert.Equal(1, readOnly[0]);
        Assert.Equal(2, readOnly[1]);
        Assert.True(((IList<int>)readOnly).IsReadOnly);
    }

    [Fact]
    public void AsReadOnly_ReflectsChangesToOriginalList()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        var readOnly = list.AsReadOnly();

        list.Add(2);

        Assert.Equal(2, readOnly.Count);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Enumeration
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void Foreach_IteratesAllElements()
    {
        NotifyingList<int> list = [];
        list.Add(10);
        list.Add(20);
        list.Add(30);
        List<int> collected = [];

        foreach (var item in list)
        {
            collected.Add(item);
        }

        Assert.Equal([10, 20, 30], collected);
    }

    [Fact]
    public void GetEnumerator_ReturnsWorkingEnumerator()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);

        using var enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(2, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void LinqOperations_WorkCorrectly()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var sum = list.Sum();
        var filtered = list.Where(x => x > 1).ToArray();

        Assert.Equal(6, sum);
        Assert.Equal([2, 3], filtered);
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    // Edge cases
    ////------------------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void EmptyList_Remove_ReturnsFalse()
    {
        NotifyingList<int> list = [];

        var result = list.Remove(42);

        Assert.False(result);
    }

    [Fact]
    public void EmptyList_Contains_ReturnsFalse()
    {
        NotifyingList<int> list = [];

        Assert.DoesNotContain(1, list);
    }

    [Fact]
    public void EmptyList_IndexOf_ReturnsNegativeOne()
    {
        NotifyingList<int> list = [];

        Assert.Equal(-1, list.IndexOf(1));
    }

    [Fact]
    public void EmptyList_Clear_DoesNotThrow()
    {
        NotifyingList<int> list = [];

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void EmptyList_FindIndex_ReturnsNegativeOne()
    {
        NotifyingList<int> list = [];

        Assert.Equal(-1, list.FindIndex(x => x == 1));
    }

    [Fact]
    public void EmptyList_RemoveAll_ReturnsZero()
    {
        NotifyingList<int> list = [];

        var removed = list.RemoveAll(_ => true);

        Assert.Equal(0, removed);
    }

    [Fact]
    public void EmptyList_ToArray_ReturnsEmptyArray()
    {
        NotifyingList<int> list = [];

        var array = list.ToArray();

        Assert.Empty(array);
    }

    [Fact]
    public void SingleElement_RemoveAt_LeavesEmptyList()
    {
        NotifyingList<int> list = [];
        list.Add(42);

        list.RemoveAt(0);

        Assert.Empty(list);
    }

    [Fact]
    public void SingleElement_Clear_LeavesEmptyList()
    {
        NotifyingList<int> list = [];
        list.Add(42);

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void DuplicateValues_Remove_RemovesFirstOccurrence()
    {
        NotifyingList<int> list = [];
        list.Add(5);
        list.Add(5);
        list.Add(5);

        list.Remove(5);

        Assert.Equal(2, list.Count);
        Assert.Equal(5, list[0]);
        Assert.Equal(5, list[1]);
    }

    [Fact]
    public void DuplicateValues_IndexOf_ReturnsFirstIndex()
    {
        NotifyingList<int> list = [];
        list.Add(5);
        list.Add(5);
        list.Add(5);

        Assert.Equal(0, list.IndexOf(5));
    }

    [Fact]
    public void DuplicateValues_Contains_ReturnsTrue()
    {
        NotifyingList<int> list = [];
        list.Add(5);
        list.Add(5);

        Assert.Contains(5, list);
    }

    [Fact]
    public void Clear_FiresRemoveEventsForEachItem()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);
        List<int> removedItems = [];
        list.ItemRemoved += (_, e) =>
        {
            removedItems.Add(e.Item);
        };

        list.Clear();

        Assert.Equal(3, removedItems.Count);
        Assert.Contains(1, removedItems);
        Assert.Contains(2, removedItems);
        Assert.Contains(3, removedItems);
    }

    [Fact]
    public void Clear_WithCancelledRemoval_RetainsProtectedItems()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.ItemRemove += (_, e) =>
        {
            if (e.Item == 2)
            {
                e.Cancel = true;
            }
        };

        list.Clear();

        Assert.Single(list);
        Assert.Equal(2, list[0]);
    }

    [Fact]
    public void AddRange_FiresEventsForEachItem()
    {
        NotifyingList<int> list = [];
        List<int> addedItems = [];
        list.ItemAdded += (_, e) =>
        {
            addedItems.Add(e.Item);
        };

        list.AddRange([10, 20, 30]);

        Assert.Equal([10, 20, 30], addedItems);
    }

    [Fact]
    public void ReferenceType_WorksCorrectly()
    {
        NotifyingList<string> list = [];
        list.Add("hello");
        list.Add("world");

        Assert.Equal(2, list.Count);
        Assert.Equal("hello", list[0]);
        Assert.Contains("world", list);
    }

    [Fact]
    public void NullableReferenceType_CanAddNull()
    {
        NotifyingList<string?> list = [];

        list.Add(null);

        Assert.Single(list);
        Assert.Null(list[0]);
    }

    [Fact]
    public void MultipleEventHandlers_AllFire()
    {
        NotifyingList<int> list = [];
        var handler1Called = false;
        var handler2Called = false;
        list.ItemAdded += (_, _) =>
        {
            handler1Called = true;
        };
        list.ItemAdded += (_, _) =>
        {
            handler2Called = true;
        };

        list.Add(1);

        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public void ItemAdd_FirstHandlerCancels_SubsequentHandlersDoNotFire()
    {
        NotifyingList<int> list = [];
        var handler2Called = false;
        list.ItemAdd += (_, e) =>
        {
            e.Cancel = true;
        };
        list.ItemAdd += (_, _) =>
        {
            handler2Called = true;
        };

        list.Add(1);

        Assert.False(handler2Called);
        Assert.Empty(list);
    }

    [Fact]
    public void ItemRemove_FirstHandlerCancels_SubsequentHandlersDoNotFire()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        var handler2Called = false;
        list.ItemRemove += (_, e) =>
        {
            e.Cancel = true;
        };
        list.ItemRemove += (_, _) =>
        {
            handler2Called = true;
        };

        list.Remove(1);

        Assert.False(handler2Called);
        Assert.Single(list);
    }

    [Fact]
    public void ItemReplace_FirstHandlerCancels_SubsequentHandlersDoNotFire()
    {
        NotifyingList<int> list = [];
        list.Add(1);
        var handler2Called = false;
        list.ItemReplace += (_, e) =>
        {
            e.Cancel = true;
        };
        list.ItemReplace += (_, _) =>
        {
            handler2Called = true;
        };

        list[0] = 99;

        Assert.False(handler2Called);
        Assert.Equal(1, list[0]);
    }
}
