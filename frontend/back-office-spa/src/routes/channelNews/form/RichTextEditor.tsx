import React, { useEffect, useRef } from 'react';
import { Button, ButtonGroup } from 'react-bootstrap';

interface RichTextEditorProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

const commands = [
  ['bold', 'Bold'],
  ['italic', 'Italic'],
  ['insertUnorderedList', 'Bulleted list'],
  ['insertOrderedList', 'Numbered list'],
] as const;

export default function RichTextEditor({
  value,
  onChange,
  disabled = false,
}: RichTextEditorProps): React.ReactElement {
  const editorRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (editorRef.current && editorRef.current.innerHTML !== value) {
      editorRef.current.innerHTML = value;
    }
  }, [value]);

  const runCommand = (command: string) => {
    editorRef.current?.focus();
    document.execCommand(command, false);
    onChange(editorRef.current?.innerHTML ?? '');
  };

  return (
    <div className="border rounded">
      <ButtonGroup aria-label="Text formatting" className="p-2 border-bottom">
        {commands.map(([command, label]) => (
          <Button
            key={command}
            type="button"
            variant="outline-secondary"
            size="sm"
            onMouseDown={event => event.preventDefault()}
            onClick={() => runCommand(command)}
            disabled={disabled}
          >
            {label}
          </Button>
        ))}
      </ButtonGroup>
      <div
        ref={editorRef}
        className="p-3"
        contentEditable={!disabled}
        role="textbox"
        aria-multiline="true"
        aria-label="ChannelNews HTML body"
        data-testid="channelnews-editor"
        onInput={event => onChange(event.currentTarget.innerHTML)}
        suppressContentEditableWarning
        style={{ minHeight: '16rem' }}
      />
    </div>
  );
}
