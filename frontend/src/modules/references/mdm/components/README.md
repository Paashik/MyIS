# ItemGroupTreeFilter Component

## Overview

The `ItemGroupTreeFilter` component provides a tree-based interface for browsing and selecting item groups from the MDM (Master Data Management) module. It's designed to be used as a filter component for nomenclature lists.

## Features

- **Hierarchical Tree View**: Displays item groups in a hierarchical tree structure
- **Search Functionality**: Allows searching groups by name or abbreviation
- **Selection Callback**: Provides callback when a group is selected
- **Loading States**: Shows loading spinner during data fetch
- **Error Handling**: Displays error messages if data loading fails
- **Empty State**: Shows appropriate message when no groups are available

## Installation

The component is already included in the MDM module. No additional installation is required.

## Usage

### Basic Usage

```tsx
import { ItemGroupTreeFilter } from "../components/ItemGroupTreeFilter";

const MyComponent = () => {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [selectedGroupName, setSelectedGroupName] = useState<string | null>(null);

  const handleGroupSelect = (groupId: string | null, groupName: string | null) => {
    setSelectedGroupId(groupId);
    setSelectedGroupName(groupName);
    // Use the selected group for filtering your nomenclature list
  };

  return (
    <ItemGroupTreeFilter 
      onGroupSelect={handleGroupSelect}
      selectedGroupId={selectedGroupId}
    />
  );
};
```

### Props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `onGroupSelect` | `(groupId: string | null, groupName: string | null) => void` | Optional | Callback function called when a group is selected. Receives group ID and name. |
| `selectedGroupId` | `string | null` | Optional | Currently selected group ID for controlled component behavior. |
| `placeholder` | `string` | Optional | Placeholder text for the search input. Default: "Поиск по группам". |

### Example with Filtering

```tsx
import { ItemGroupTreeFilter } from "../components/ItemGroupTreeFilter";
import { useState, useEffect } from "react";

const NomenclatureList = () => {
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);
  const [filteredItems, setFilteredItems] = useState<any[]>([]);
  const [allItems, setAllItems] = useState<any[]>([]);

  // Load all items from API
  useEffect(() => {
    const loadItems = async () => {
      const items = await fetchItemsFromApi();
      setAllItems(items);
      setFilteredItems(items);
    };
    loadItems();
  }, []);

  const handleGroupSelect = (groupId: string | null) => {
    setSelectedGroupId(groupId);
    
    if (groupId) {
      // Filter items by selected group
      const filtered = allItems.filter(item => item.groupId === groupId);
      setFilteredItems(filtered);
    } else {
      // Show all items
      setFilteredItems(allItems);
    }
  };

  return (
    <div style={{ display: "flex" }}>
      <div style={{ width: 300, marginRight: 16 }}>
        <ItemGroupTreeFilter 
          onGroupSelect={handleGroupSelect}
          selectedGroupId={selectedGroupId}
          placeholder="Поиск групп..."
        />
      </div>
      
      <div style={{ flex: 1 }}>
        {/* Render your filtered items here */}
        {filteredItems.map(item => (
          <div key={item.id}>{item.name}</div>
        ))}
      </div>
    </div>
  );
};
```

## Integration with Existing Components

The component is designed to work seamlessly with the existing MDM API and data structures. It uses the same `MdmItemGroupReferenceDto` type that's used throughout the MDM module.

## Styling

The component has built-in styling that matches the Ant Design theme. You can override styles by wrapping the component or using CSS-in-JS solutions.

## Performance

- **Lazy Loading**: The component loads all groups at once for better user experience
- **Efficient Search**: Search is performed client-side for instant results
- **Tree Optimization**: Uses Ant Design's optimized Tree component for large datasets

## Error Handling

The component handles errors gracefully:
- Network errors during data loading
- Empty data states
- Invalid group structures

## Future Enhancements

- Virtual scrolling for very large group hierarchies
- Server-side search for better performance with huge datasets
- Customizable tree node rendering
- Drag-and-drop support for group reorganization (admin feature)

## API Reference

The component uses the existing MDM API endpoint:
- `GET /api/admin/references/mdm/item-groups` - Fetches all item groups

## Dependencies

- React 18+
- Ant Design 5+
- TypeScript 5+

## Browser Support

The component works in all modern browsers supported by the MyIS application.