import { useState } from 'react';
import { useSearchParams } from 'react-router';
import { Table, Button, Form, InputGroup } from 'react-bootstrap';
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getPaginationRowModel,
  getFilteredRowModel,
  ColumnDef,
  flexRender,
  SortingState,
} from '@tanstack/react-table';
import { fuzzyFilter } from './tableUtils';

interface GenericTableProps<T extends object> {
  data: T[];
  columns: ColumnDef<T, unknown>[];
  pageSize?: number;
  searchPlaceholder?: string;
  emptyMessage?: string;
  previousButtonText?: string;
  nextButtonText?: string;
}

function GenericTable<T extends object>({
  data,
  columns,
  pageSize = 10,
  searchPlaceholder = 'Search...',
  emptyMessage = 'No elements',
  previousButtonText = 'Previous',
  nextButtonText = 'Next',
}: GenericTableProps<T>) {
  // State for react-table
  const [sorting, setSorting] = useState<SortingState>([]);
  const [globalFilter, setGlobalFilter] = useState('');
  const [searchParams, setSearchParams] = useSearchParams();

  const pageIndex = Math.max(0, parseInt(searchParams.get('page') || '1', 10) - 1);

  // Table configuration
  const table = useReactTable<T>({
    data,
    columns: columns as ColumnDef<T, unknown>[], // Type assertion needed due to compatibility issues with react-table types
    state: {
      sorting,
      globalFilter,
      pagination: { pageIndex, pageSize },
    },
    onSortingChange: setSorting,
    onGlobalFilterChange: setGlobalFilter,
    onPaginationChange: updater => {
      const next = typeof updater === 'function' ? updater({ pageIndex, pageSize }) : updater;
      setSearchParams(prev => {
        const params = new URLSearchParams(prev);
        if (next.pageIndex === 0) {
          params.delete('page');
        } else {
          params.set('page', String(next.pageIndex + 1));
        }
        return params;
      }, { replace: true });
    },
    globalFilterFn: fuzzyFilter,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    manualPagination: false,
  });

  return (
    <>
      <div className="mb-3">
        <InputGroup>
          <Form.Control
            value={globalFilter ?? ''}
            onChange={e => {
            setGlobalFilter(e.target.value);
            setSearchParams(prev => {
              const params = new URLSearchParams(prev);
              params.delete('page');
              return params;
            }, { replace: true });
          }}
            placeholder={searchPlaceholder}
          />
        </InputGroup>
      </div>

      <Table striped bordered hover>
        <thead>
          {table.getHeaderGroups().map(headerGroup => (
            <tr key={headerGroup.id}>
              {headerGroup.headers.map(header => (
                <th key={header.id} colSpan={header.colSpan}>
                  {header.isPlaceholder ? null : (
                    <div
                      {...{
                        className: header.column.getCanSort() ? 'cursor-pointer select-none' : '',
                        onClick: header.column.getToggleSortingHandler(),
                      }}
                    >
                      {flexRender(header.column.columnDef.header, header.getContext())}
                      {{
                        asc: ' 🔼',
                        desc: ' 🔽',
                      }[header.column.getIsSorted() as string] ?? null}
                    </div>
                  )}
                </th>
              ))}
            </tr>
          ))}
        </thead>
        <tbody>
          {table.getRowModel().rows.length > 0 ? (
            table.getRowModel().rows.map(row => (
              <tr key={row.id}>
                {row.getVisibleCells().map(cell => (
                  <td key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</td>
                ))}
              </tr>
            ))
          ) : (
            <tr>
              <td colSpan={table.getHeaderGroups()[0].headers.length} className="text-center">
                {emptyMessage}
              </td>
            </tr>
          )}
        </tbody>
      </Table>
      {table.getPageCount() > 1 && (
        <div className="d-flex justify-content-between align-items-center">
          <Button
            variant="link"
            disabled={!table.getCanPreviousPage()}
            onClick={() => table.previousPage()}
          >
            {previousButtonText}
          </Button>
          <span>
            Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
          </span>
          <Button
            variant="link"
            disabled={!table.getCanNextPage()}
            onClick={() => table.nextPage()}
          >
            {nextButtonText}
          </Button>
        </div>
      )}
    </>
  );
}

export default GenericTable;
