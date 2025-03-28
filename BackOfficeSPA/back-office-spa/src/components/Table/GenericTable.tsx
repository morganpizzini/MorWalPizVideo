import { useState } from 'react';
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

  // Table configuration
  const table = useReactTable<T>({
    data,
    columns: columns as ColumnDef<T, unknown>[], // Type assertion needed due to compatibility issues with react-table types
    state: {
      sorting,
      globalFilter,
    },
    onSortingChange: setSorting,
    onGlobalFilterChange: setGlobalFilter,
    globalFilterFn: fuzzyFilter,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    initialState: {
      pagination: {
        pageSize,
      },
    },
  });

  return (
    <>
      <div className="mb-3">
        <InputGroup>
          <Form.Control
            value={globalFilter ?? ''}
            onChange={e => setGlobalFilter(e.target.value)}
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
