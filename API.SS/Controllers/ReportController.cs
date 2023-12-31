﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EF.Core.ShoppersStore.ShoppersStoreDB.Models;
using ServiceLib.ShoppersStore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http.Headers;
using ServiceLib.ShoppersStore.DTO;
using Microsoft.AspNetCore.Authorization;

namespace API.SS.Controllers
{
    [Authorize("Manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepo;

        public ReportController(IReportRepository reportRepo)
        {
            _reportRepo = reportRepo;
        }

        [HttpGet]
        [Route("productsWithImage")]
        public async Task<IActionResult> GetProductsWithImage()
        {
            try
            {
                var allProducts = await _reportRepo.GetProductsWithImage();
                return Ok(allProducts);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("monthlyStoreWise")]
        public async Task<IActionResult> MonthlyStoreWise(MonthlyTotalSalesData data)
        {
            try
            {
                List<MonthlyTotalSalesData> datas = await _reportRepo.MonthlyStoreWise(data);
                return Ok(datas);
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid Data !");
            }
        }

        [HttpPost]
        [Route("monthlyProductWise")]
        public async Task<IActionResult> MonthlyProductWise(YearlyProductWiseSalesData data)
        {
            try
            {
                List<YearlyProductWiseSalesData> datas = await _reportRepo.MonthlyProductWise(data);
                return Ok(datas);
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid Data !");
            }
        }

        [HttpPost]
        [Route("selectedProductWise")]
        public async Task<IActionResult> SelectedProductWise(MonthlyProductWiseSalesData data)
        {
            try
            {
                List<MonthlyProductWiseSalesData> datas = await _reportRepo.SelectedProductWise(data);
                return Ok(datas);
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid Data !");
            }
        }

        [HttpPost]
        [Route("discountWise")]
        public async Task<IActionResult> DiscountWise(ProductDiscountSalesData data)
        {
            try
            {
                List<ProductDiscountSalesData> datas = await _reportRepo.DiscountWise(data);
                return Ok(datas);
            }
            catch (Exception ex)
            {
                return BadRequest("Invalid Data !");
            }
        }

    }
}
