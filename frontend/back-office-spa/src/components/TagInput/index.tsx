import React, { useId, useMemo, useRef, useState } from 'react';
import { Badge, Form } from 'react-bootstrap';
import {
  MAX_TAGS_PER_CONTENT,
  MAX_TAG_LENGTH,
  containsIgnoreCase,
  equalsIgnoreCase,
} from './tagRules';
import './index.css';

export interface TagInputProps {
  /** Current tag values, in display order. */
  value: string[];
  /** Called with the new tag list whenever a tag is added or removed. */
  onChange: (tags: string[]) => void;
  /** Bounded suggestion pool used for autocomplete. */
  suggestions?: string[];
  label?: string;
  helpText?: string;
  placeholder?: string;
  disabled?: boolean;
  id?: string;
  maxTags?: number;
  maxTagLength?: number;
}

export default function TagInput({
  value,
  onChange,
  suggestions = [],
  label,
  helpText,
  placeholder = 'Add a tag and press Enter',
  disabled = false,
  id,
  maxTags = MAX_TAGS_PER_CONTENT,
  maxTagLength = MAX_TAG_LENGTH,
}: TagInputProps) {
  const generatedId = useId();
  const inputId = id ?? `tag-input-${generatedId}`;
  const listboxId = `${inputId}-listbox`;

  const [draft, setDraft] = useState('');
  const [activeSuggestion, setActiveSuggestion] = useState(-1);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const isFull = value.length >= maxTags;

  const visibleSuggestions = useMemo(() => {
    const query = draft.trim();
    return suggestions
      .filter(suggestion => !value.some(tag => equalsIgnoreCase(tag, suggestion)))
      .filter(suggestion => query.length === 0 || containsIgnoreCase(suggestion, query))
      .slice(0, 8);
  }, [suggestions, value, draft]);

  const isSuggestionListOpen = !disabled && !isFull && draft.trim().length > 0 && visibleSuggestions.length > 0;

  const commitTag = (candidate: string): void => {
    const trimmed = candidate.trim();
    if (trimmed.length === 0) {
      setDraft('');
      return;
    }
    if (trimmed.length > maxTagLength) {
      setError(`Tags cannot exceed ${maxTagLength} characters.`);
      return;
    }
    if (value.some(tag => equalsIgnoreCase(tag, trimmed))) {
      // Case-insensitive dedupe: keep the existing display casing and clear the draft.
      setDraft('');
      setActiveSuggestion(-1);
      setError(null);
      return;
    }
    if (value.length >= maxTags) {
      setError(`A maximum of ${maxTags} tags is allowed.`);
      return;
    }

    onChange([...value, trimmed]);
    setDraft('');
    setActiveSuggestion(-1);
    setError(null);
  };

  const removeTag = (tagToRemove: string): void => {
    onChange(value.filter(tag => tag !== tagToRemove));
    setError(null);
  };

  const handleDraftChange = (event: React.ChangeEvent<HTMLInputElement>): void => {
    const next = event.target.value;
    setActiveSuggestion(-1);
    setError(null);

    // Comma creates a tag, including when several are pasted at once.
    if (next.includes(',')) {
      const segments = next.split(',');
      const trailing = segments.pop() ?? '';
      segments.forEach(segment => commitTag(segment));
      setDraft(trailing.trimStart());
      return;
    }

    setDraft(next);
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>): void => {
    switch (event.key) {
      case 'Enter':
        event.preventDefault();
        if (isSuggestionListOpen && activeSuggestion >= 0) {
          commitTag(visibleSuggestions[activeSuggestion]);
          return;
        }
        commitTag(draft);
        return;
      case 'Tab':
        if (isSuggestionListOpen && activeSuggestion >= 0) {
          event.preventDefault();
          commitTag(visibleSuggestions[activeSuggestion]);
        }
        return;
      case 'ArrowDown':
        if (!isSuggestionListOpen) return;
        event.preventDefault();
        setActiveSuggestion(previous => (previous + 1) % visibleSuggestions.length);
        return;
      case 'ArrowUp':
        if (!isSuggestionListOpen) return;
        event.preventDefault();
        setActiveSuggestion(previous =>
          previous <= 0 ? visibleSuggestions.length - 1 : previous - 1
        );
        return;
      case 'Escape':
        if (activeSuggestion >= 0) {
          event.preventDefault();
          setActiveSuggestion(-1);
        }
        return;
      case 'Backspace':
        if (draft.length === 0 && value.length > 0) {
          event.preventDefault();
          removeTag(value[value.length - 1]);
        }
        return;
      default:
    }
  };

  return (
    <Form.Group className="mb-3">
      {label && <Form.Label htmlFor={inputId}>{label}</Form.Label>}

      <div className="position-relative">
        <div
          className={[
            'tag-input__control',
            error ? 'tag-input__control--invalid' : '',
            disabled ? 'tag-input__control--disabled' : '',
          ]
            .filter(Boolean)
            .join(' ')}
          onClick={() => inputRef.current?.focus()}
        >
          {value.map(tag => (
            <Badge key={tag} bg="primary" className="tag-input__chip">
              <span className="tag-input__chip-text">{tag}</span>
              {!disabled && (
                <button
                  type="button"
                  className="tag-input__chip-remove"
                  onClick={event => {
                    event.stopPropagation();
                    removeTag(tag);
                  }}
                  aria-label={`Remove tag ${tag}`}
                >
                  &times;
                </button>
              )}
            </Badge>
          ))}

          <input
            ref={inputRef}
            id={inputId}
            type="text"
            className="tag-input__field"
            value={draft}
            disabled={disabled || isFull}
            placeholder={isFull ? `Tag limit of ${maxTags} reached` : placeholder}
            maxLength={maxTagLength}
            onChange={handleDraftChange}
            onKeyDown={handleKeyDown}
            onBlur={() => {
              commitTag(draft);
              setActiveSuggestion(-1);
            }}
            role="combobox"
            aria-expanded={isSuggestionListOpen}
            aria-controls={listboxId}
            aria-autocomplete="list"
            aria-activedescendant={
              isSuggestionListOpen && activeSuggestion >= 0
                ? `${listboxId}-option-${activeSuggestion}`
                : undefined
            }
            aria-describedby={`${inputId}-help`}
          />
        </div>

        {isSuggestionListOpen && (
          <ul className="tag-input__suggestions" id={listboxId} role="listbox" aria-label="Tag suggestions">
            {visibleSuggestions.map((suggestion, index) => (
              <li
                key={suggestion}
                id={`${listboxId}-option-${index}`}
                role="option"
                aria-selected={index === activeSuggestion}
                className={[
                  'tag-input__suggestion',
                  index === activeSuggestion ? 'tag-input__suggestion--active' : '',
                ]
                  .filter(Boolean)
                  .join(' ')}
                onMouseDown={event => {
                  // Commit before the input blur handler runs.
                  event.preventDefault();
                  commitTag(suggestion);
                }}
              >
                {suggestion}
              </li>
            ))}
          </ul>
        )}
      </div>

      {error && (
        <div className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      )}

      <Form.Text id={`${inputId}-help`} className="text-muted">
        {helpText ??
          `Press Enter or comma to add a tag. Up to ${maxTags} tags, ${maxTagLength} characters each.`}
      </Form.Text>
    </Form.Group>
  );
}
