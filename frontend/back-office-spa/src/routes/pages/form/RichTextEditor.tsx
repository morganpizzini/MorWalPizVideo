import React, { useEffect, useRef } from 'react';
import { Button, ButtonGroup } from 'react-bootstrap';

interface RichTextEditorProps { value: string; onChange: (value: string) => void; disabled?: boolean; }

const commands = [['bold', 'Bold'], ['italic', 'Italic'], ['insertUnorderedList', 'Bulleted list'], ['insertOrderedList', 'Numbered list']] as const;
const columnCounts = [1, 2, 3] as const;

export const createColumnLayout = (columnCount: number): string => {
  const safeColumnCount = Math.max(1, Math.min(3, Math.trunc(columnCount)));
  const columns = Array.from({ length: safeColumnCount }, () => '<div class="page-column"><p><br></p></div>').join('');
  return `<div class="page-columns">${columns}</div>`;
};

export default function RichTextEditor({ value, onChange, disabled = false }: RichTextEditorProps): React.ReactElement {
  const editorRef = useRef<HTMLDivElement>(null);
  useEffect(() => { if (editorRef.current && editorRef.current.innerHTML !== value) editorRef.current.innerHTML = value; }, [value]);
  const runCommand = (command: string) => { editorRef.current?.focus(); document.execCommand(command, false); onChange(editorRef.current?.innerHTML ?? ''); };
  const insertColumnLayout = (columnCount: number) => {
    const layout = createColumnLayout(columnCount);
    editorRef.current?.focus();
    if (typeof document.execCommand === 'function') document.execCommand('insertHTML', false, layout);
    else if (editorRef.current) editorRef.current.innerHTML += layout;
    onChange(editorRef.current?.innerHTML ?? '');
  };
  return <div className="border rounded"><ButtonGroup aria-label="Text formatting" className="p-2 border-bottom">{commands.map(([command, label]) => <Button key={command} type="button" variant="outline-secondary" size="sm" onMouseDown={event => event.preventDefault()} onClick={() => runCommand(command)} disabled={disabled}>{label}</Button>)}{columnCounts.map(columnCount => <Button key={columnCount} type="button" variant="outline-secondary" size="sm" onMouseDown={event => event.preventDefault()} onClick={() => insertColumnLayout(columnCount)} disabled={disabled} aria-label={`Insert ${columnCount} column layout`}>{columnCount} column{columnCount === 1 ? '' : 's'}</Button>)}</ButtonGroup><div ref={editorRef} className="p-3" contentEditable={!disabled} role="textbox" aria-multiline="true" aria-label="Page HTML body" data-testid="page-editor" onInput={event => onChange(event.currentTarget.innerHTML)} suppressContentEditableWarning style={{ minHeight: '16rem' }} /></div>;
}