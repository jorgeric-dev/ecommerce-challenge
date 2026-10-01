import { ProductSortField, SortDirection } from '../../core/api/api.models';

export interface SortOption {
  id: string;
  label: string;
  sortBy: ProductSortField;
  sortDirection: SortDirection;
}

export const PRODUCT_SORT_OPTIONS: readonly SortOption[] = [
  { id: 'name-asc', label: 'Name (A–Z)', sortBy: 'Name', sortDirection: 'Asc' },
  { id: 'name-desc', label: 'Name (Z–A)', sortBy: 'Name', sortDirection: 'Desc' },
  { id: 'price-asc', label: 'Price: low to high', sortBy: 'Price', sortDirection: 'Asc' },
  { id: 'price-desc', label: 'Price: high to low', sortBy: 'Price', sortDirection: 'Desc' },
  { id: 'newest', label: 'Newest first', sortBy: 'CreatedAt', sortDirection: 'Desc' },
];

export const PAGE_SIZE_OPTIONS = [12, 24, 48];
