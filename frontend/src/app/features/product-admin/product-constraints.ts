export const PRODUCT_CONSTRAINTS = {
  nameMaxLength: 200,
  skuMaxLength: 50,
  skuPattern: /^[A-Za-z0-9][A-Za-z0-9_-]*$/,
  descriptionMaxLength: 1000,
  minPrice: 0.01,
  maxPrice: 1_000_000,
  priceDecimals: 2,
  minStock: 0,
  maxStock: 1_000_000,
  minWeightKg: 0,
  maxWeightKg: 10_000,
  weightDecimals: 3,
} as const;
