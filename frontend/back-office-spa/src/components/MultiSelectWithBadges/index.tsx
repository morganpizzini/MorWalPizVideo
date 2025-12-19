import React, { useState } from 'react';
import { Form, Badge, Button } from 'react-bootstrap';
import './index.css';

export interface MultiSelectWithBadgesProps<T> {
  /** Available items to select from */
  items: T[];
  /** Currently selected items */
  selectedItems: T[];
  /** Callback when selection changes */
  onSelectionChange: (selected: T[]) => void;
  /** Function to extract unique ID from item */
  getItemId: (item: T) => string;
  /** Function to get display text for item */
  getItemDisplay: (item: T) => string;
  /** Optional placeholder text for select */
  placeholder?: string;
  /** Optional label for the form group */
  label?: string;
  /** Optional help text */
  helpText?: string;
  /** Whether the field is disabled */
  disabled?: boolean;
  /** Error message to display */
  error?: string | string[];
}

export default function MultiSelectWithBadges<T>({
  items,
  selectedItems,
  onSelectionChange,
  getItemId,
  getItemDisplay,
  placeholder = 'Select an item...',
  label,
  helpText,
  disabled = false,
  error
}: MultiSelectWithBadgesProps<T>) {
  const [selectedValue, setSelectedValue] = useState<string>('');

  // Filter out already selected items
  const availableItems = items.filter(item => 
    !selectedItems.some(selected => getItemId(selected) === getItemId(item))
  );

  const handleSelectChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const selectedId = e.target.value;
    setSelectedValue('');
    
    if (!selectedId) return;

    const selectedItem = items.find(item => getItemId(item) === selectedId);
    if (selectedItem) {
      onSelectionChange([...selectedItems, selectedItem]);
    }
  };

  const handleRemoveItem = (itemToRemove: T) => {
    const newSelection = selectedItems.filter(item => 
      getItemId(item) !== getItemId(itemToRemove)
    );
    onSelectionChange(newSelection);
  };

  return (
    <Form.Group className="mb-3">
      {label && (
        <Form.Label>
          {label}
        </Form.Label>
      )}
      
      <Form.Select
        value={selectedValue}
        onChange={handleSelectChange}
        disabled={disabled || availableItems.length === 0}
        isInvalid={!!error}
      >
        <option value="">
          {availableItems.length === 0 ? 'No more items available' : placeholder}
        </option>
        {availableItems.map(item => (
          <option key={getItemId(item)} value={getItemId(item)}>
            {getItemDisplay(item)}
          </option>
        ))}
      </Form.Select>

      {error && (
        <div className="invalid-feedback d-block">
          {Array.isArray(error) ? error.join(', ') : error}
        </div>
      )}

      {helpText && (
        <Form.Text className="text-muted">
          {helpText}
        </Form.Text>
      )}

      {/* Selected items as badges */}
      {selectedItems.length > 0 && (
        <div className="selected-badges-container mt-2">
          {selectedItems.map(item => (
            <Badge
              key={getItemId(item)}
              bg="primary"
              className="selected-badge me-2 mb-2"
            >
              <span className="badge-text">{getItemDisplay(item)}</span>
              {!disabled && (
                <Button
                  variant="link"
                  className="badge-remove-btn"
                  onClick={() => handleRemoveItem(item)}
                  aria-label={`Remove ${getItemDisplay(item)}`}
                >
                  ×
                </Button>
              )}
            </Badge>
          ))}
        </div>
      )}
    </Form.Group>
  );
}
