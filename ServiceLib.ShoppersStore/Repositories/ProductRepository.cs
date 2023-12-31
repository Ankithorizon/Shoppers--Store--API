﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceLib.ShoppersStore.Interfaces;
using EF.Core.ShoppersStore.ShoppersStoreDB;
using EF.Core.ShoppersStore.ShoppersStoreDB.Models;
using ServiceLib.ShoppersStore.DTO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ServiceLib.ShoppersStore.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ShoppersStoreContext appDbContext;
        public ProductRepository(ShoppersStoreContext appDbContext)
        {
            this.appDbContext = appDbContext;
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {
            return await appDbContext.Categories.ToListAsync();
        }


        public async Task<Product> AddProduct(Product product)
        {
            try
            {
                // throw new Exception();

                var result = appDbContext.Products.Add(product);
                await appDbContext.SaveChangesAsync();
                return result.Entity;
            }
            catch(Exception ex)
            {
                throw ex;
            }           
        }

        // product image info save to db
        public async Task<ProductFileAddResponse> ProductFileAdd(AddProductFile addProductFile)
        {
            ProductFileAddResponse response = new ProductFileAddResponse();
            using var transaction = appDbContext.Database.BeginTransaction();
            try
            {
                // throw new Exception();

                // 1)
                ProductFile productFile = new ProductFile()
                {
                    ProductId = addProductFile.ProductId,
                    FileName = addProductFile.FileName,
                    FilePath = addProductFile.FilePath
                };
                var productFileSaved = appDbContext.ProductFiles.Add(productFile);
                await appDbContext.SaveChangesAsync();

                // 2)
                var product = await appDbContext.Products
                                    .Where(x => x.ProductId == addProductFile.ProductId).FirstOrDefaultAsync();
                if (product != null)
                {
                    product.ProductFileId = productFileSaved.Entity.ProductFileId;
                    await appDbContext.SaveChangesAsync();
                }
                else
                {
                    throw new Exception();
                }

                // commit 1 & 2
                transaction.Commit();


                response.ResponseCode = 0;
                response.ResponseMessage = "Product File Saved Successfully !";
                response.ProductFileId = productFileSaved.Entity.ProductFileId;
                response.ProductImage = productFileSaved.Entity.FileName;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                response.ResponseCode = -1;
                response.ResponseMessage = "Product File Saved Fail !";
                response.ProductFileId = 0;
                response.ProductImage = null;
            }
            return response;
        }

        public async Task<IEnumerable<ProductDTO>> GetAllProducts()
        {
            List<ProductDTO> products = new List<ProductDTO>();

            var _products = appDbContext.Products;

            if (_products != null && _products.Count() > 0)
            {
                foreach (var _product in _products)
                {
                    var _productImage = await appDbContext.ProductFiles
                                        .Where(x => x.ProductFileId == _product.ProductFileId).FirstOrDefaultAsync();
                    if (_productImage != null)
                    {
                        // products with image
                        products.Add(new ProductDTO()
                        {
                            CategoryId = _product.CategoryId,
                            Price = _product.Price,
                            ProductDesc = _product.ProductDesc,
                            ProductFileId = _product.ProductFileId,
                            ProductId = _product.ProductId,
                            ProductImage = _productImage.FileName,
                            ProductName = _product.ProductName,
                            CurrentPrice = _product.DiscountPrice > 0 ? _product.DiscountPrice : _product.Price,
                            CurrentDiscountPercentage = _product.DiscountPercentage
                        });
                    }
                    else
                    {
                        // products without image
                        products.Add(new ProductDTO()
                        {
                            CategoryId = _product.CategoryId,
                            Price = _product.Price,
                            ProductDesc = _product.ProductDesc,
                            ProductId = _product.ProductId,
                            ProductImage = null,
                            ProductName = _product.ProductName,
                            CurrentPrice = _product.DiscountPrice > 0 ? _product.DiscountPrice : _product.Price,
                            CurrentDiscountPercentage = _product.DiscountPercentage
                        });
                    }
                }
            }
            return products;
        }

        public async Task<IEnumerable<ProductDTO>> SearchProducts(string searchValue, string categoryId)
        {
            List<ProductDTO> products = new List<ProductDTO>();
            IQueryable<Product> _products = appDbContext.Products;
            List<Product> _productsByNameDesc = new List<Product>();
            List<Product> _productsByCategory = new List<Product>();


            if (searchValue != null)
            {
                _productsByNameDesc = await _products
                          .Where(x => x.ProductName.Contains(searchValue) || x.ProductDesc.Contains(searchValue)).ToListAsync();
            }
            if (categoryId != null)
            {
                try
                {
                    int catId = Int32.Parse(categoryId);

                    _productsByCategory = await _products
                             .Where(x => x.CategoryId == catId).ToListAsync();
                }
                catch (FormatException e)
                {
                    throw new Exception();
                }              
            }

            // add
            _products = (_productsByNameDesc.Concat(_productsByCategory)).AsQueryable<Product>();

            // remove duplicate
            _products = _products.Distinct(new DistinctProductComparer()).AsQueryable<Product>();


            if (_products != null && _products.Count() > 0)
            {
                foreach (var _product in _products)
                {
                    var _productImage = await appDbContext.ProductFiles
                                        .Where(x => x.ProductFileId == _product.ProductFileId).FirstOrDefaultAsync();
                    if (_productImage != null)
                    {
                        // products with image
                        products.Add(new ProductDTO()
                        {
                            CategoryId = _product.CategoryId,
                            Price = _product.Price,
                            ProductDesc = _product.ProductDesc,
                            ProductFileId = _product.ProductFileId,
                            ProductId = _product.ProductId,
                            ProductImage = _productImage.FileName,
                            ProductName = _product.ProductName,
                            CurrentPrice = _product.DiscountPrice > 0 ? _product.DiscountPrice : _product.Price
                        });
                    }
                    else
                    {
                        // products without image
                        products.Add(new ProductDTO()
                        {
                            CategoryId = _product.CategoryId,
                            Price = _product.Price,
                            ProductDesc = _product.ProductDesc,
                            ProductId = _product.ProductId,
                            ProductImage = null,
                            ProductName = _product.ProductName,
                            CurrentPrice = _product.DiscountPrice > 0 ? _product.DiscountPrice : _product.Price
                        });
                    }
                }
            }
            return products;
        }

        public async Task<ProductDTO> GetProduct(int productId)
        {
            ProductDTO product = new ProductDTO();

            var _product = await appDbContext.Products
                                .Where(x => x.ProductId == productId).FirstOrDefaultAsync();
            if (_product != null)
            {
                var _productFile = await appDbContext.ProductFiles
                                        .Where(x => x.ProductFileId == _product.ProductFileId).FirstOrDefaultAsync();

                // product with image
                if (_productFile != null)
                {
                    product.CategoryId = _product.CategoryId;
                    product.Price = _product.Price;
                    product.ProductDesc = _product.ProductDesc;
                    product.ProductFileId = _product.ProductFileId;
                    product.ProductId = _product.ProductId;
                    product.ProductImage = _productFile.FileName;
                    product.ProductName = _product.ProductName;
                    product.CurrentPrice = _product.DiscountPrice;
                    product.CurrentDiscountPercentage = _product.DiscountPercentage;
                }
                // product without image
                else
                {
                    product.CategoryId = _product.CategoryId;
                    product.Price = _product.Price;
                    product.ProductDesc = _product.ProductDesc;
                    product.ProductId = _product.ProductId;
                    product.ProductImage = null;
                    product.ProductName = _product.ProductName;
                    product.CurrentPrice = _product.DiscountPrice;
                    product.CurrentDiscountPercentage = _product.DiscountPercentage;
                }
            }
            return product;
            // return null;
        }

        public async Task<ProductDTO> EditProduct(ProductDTO product)
        {
            var _product = await appDbContext.Products.Where(x => x.ProductId == product.ProductId).FirstOrDefaultAsync();
            if (_product != null)
            {
                _product.CategoryId = product.CategoryId;
                _product.ProductName = product.ProductName;
                _product.ProductDesc = product.ProductDesc;
                _product.Price = product.Price;

                await appDbContext.SaveChangesAsync();
                return product;
            }
            else
            {
                return null;
            }
        }

        public async Task<ProductFileEditResponse> ProductFileEdit(ProductFileEditResponse _productFile)
        {
            if (_productFile.ProductFileId > 0)
            {
                // edit
                // existing product image
                var productFile = await appDbContext.ProductFiles.Where(x => x.ProductFileId == _productFile.ProductFileId).FirstOrDefaultAsync();
                if (productFile != null)
                {
                    productFile.FileName = _productFile.ProductImage;
                    productFile.FilePath = _productFile.ProductImagePath;
                    await appDbContext.SaveChangesAsync();

                    _productFile.ResponseCode = 0;
                    _productFile.ResponseMessage = "Image-EDIT : Success !";
                }
            }
            // existing product NO image
            else
            {
                // This product has NO Image and
                // User is uploading new Image for this product
                using var transaction = appDbContext.Database.BeginTransaction();

                try
                {
                    // 1) add @ ProductFiles                          
                    var productFileSaved = await appDbContext.ProductFiles.AddAsync(new ProductFile()
                    {
                        FileName = _productFile.ProductImage,
                        FilePath = _productFile.ProductImagePath,
                        ProductId = _productFile.ProductId,
                    });
                    await appDbContext.SaveChangesAsync();

                    // check for transaction rollback
                    // throw new Exception();

                    // 2) update @ Products
                    var product_ = await appDbContext.Products
                                    .Where(x => x.ProductId == _productFile.ProductId).FirstOrDefaultAsync();
                    product_.ProductFileId = productFileSaved.Entity.ProductFileId;
                    await appDbContext.SaveChangesAsync();

                    _productFile.ResponseCode = 0;
                    _productFile.ResponseMessage = "Image-EDIT : Success !";

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    _productFile.ResponseCode = -1;
                    _productFile.ResponseMessage = "Image-EDIT : Fail !";
                }
            }
            return _productFile;
        }

        public async Task<ProductDiscountDTO> SetProductDiscount(ProductDiscountDTO discount)
        {
            discount.APIResponse = new APIResponse();

            var _product = await appDbContext.Products
                                .Where(x => x.ProductId == discount.ProductId).FirstOrDefaultAsync();
            if (_product != null)
            {
                // update @ Products
                _product.DiscountPercentage = discount.DiscountPercentage;
                _product.DiscountPrice = _product.Price - ((_product.Price * discount.DiscountPercentage) / 100);

                // insert @ DiscountHistories
                // check if never discount has been set for this product
                var discountSetFound = await appDbContext.DiscountHistories
                                .Where(x => x.ProductId == discount.ProductId).FirstOrDefaultAsync();
                if (discountSetFound != null)
                {
                    // ever discount has been set for this product
                    // find the last record
                    var lastDiscountSet = appDbContext.DiscountHistories
                                            .Where(x => x.ProductId == discount.ProductId)
                                            .OrderBy(x => x.DiscountHistoryId);
                    if (lastDiscountSet != null && lastDiscountSet.Count() > 0)
                    {
                        // update DiscountEffectiveEnd 
                        // lastDiscountSet.LastOrDefault().DiscountEffectiveEnd = DateTime.Now.AddDays(-1);
                        lastDiscountSet.LastOrDefault().DiscountEffectiveEnd = DateTime.Now.AddDays(0);
                    }
                    // insert @ DiscountHistories
                    await appDbContext.DiscountHistories.AddAsync(new DiscountHistory()
                    {
                        DiscountEffectiveBegin = DateTime.Now,
                        DiscountPercentage = discount.DiscountPercentage,
                        ProductId = discount.ProductId,
                        DiscountEffectiveEnd = null
                    });
                }
                else
                {
                    // never discount has been set for this product
                    // insert @ DiscountHistories
                    await appDbContext.DiscountHistories.AddAsync(new DiscountHistory()
                    {
                        DiscountEffectiveBegin = DateTime.Now,
                        DiscountPercentage = discount.DiscountPercentage,
                        ProductId = discount.ProductId
                    });
                }

                await appDbContext.SaveChangesAsync();

                discount.APIResponse.ResponseCode = 0;
                discount.APIResponse.ResponseMessage = "Discount Applied Successfully !";
                discount.DiscountPrice = _product.DiscountPrice;
                discount.Price = _product.Price;
            }
            else
            {
                discount.APIResponse.ResponseCode = -1;
                discount.APIResponse.ResponseMessage = "Product Not Found !";
            }
            return discount;
        }

        public async Task<bool> ResetProductDiscount(int productId)
        {
            var _product = await appDbContext.Products
                                .Where(x => x.ProductId == productId).FirstOrDefaultAsync();
            if (_product != null)
            {
                // update @ Products
                _product.DiscountPercentage = 0;
                _product.DiscountPrice = 0;

                // @ DiscountHistories
                // check if never discount has been set for this product
                var discountSetFound = await appDbContext.DiscountHistories
                                .Where(x => x.ProductId == productId).FirstOrDefaultAsync();
                if (discountSetFound != null)
                {
                    // ever discount has been set for this product
                    // find the last record
                    var lastDiscountSet = appDbContext.DiscountHistories
                                            .Where(x => x.ProductId == productId)
                                            .OrderBy(x => x.DiscountHistoryId);
                    if (lastDiscountSet != null && lastDiscountSet.Count() > 0)
                    {
                        // update DiscountEffectiveEnd                         
                        lastDiscountSet.LastOrDefault().DiscountEffectiveEnd = DateTime.Now.AddDays(0);
                    }
                }
                await appDbContext.SaveChangesAsync();
                return true;
            }
            else
                return false;           
        }
    }
}
