﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using DD_Business.Repository.IRepository;
using DD_DataAccess;
using DD_Models;

namespace DDWeb_API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        public OrderController(IOrderRepository orderRepository, IEmailSender emailSender, UserManager<ApplicationUser> userManager)
        {
            _orderRepository = orderRepository;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAll(string? userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }
            var userRoles = await _userManager.GetRolesAsync(user);
            if(userRoles == null || !userRoles.Any())
            {
                throw new ArgumentNullException(nameof(userRoles));
            }

            bool isAdmin = userRoles.Any(x => x == DD_Common.SD.Role_Admin);
            if (isAdmin)
            {
                userId = null;
            }
            return Ok(await _orderRepository.GetAll(userId));
        }

        [HttpGet("{orderHeaderId}")]
        public async Task<IActionResult> Get(int? orderHeaderId)
        {
            if (orderHeaderId==null || orderHeaderId==0)
            {
                return BadRequest(new ErrorModelDTO()
                {
                    ErrorMessage="Invalid Id",
                    StatusCode=StatusCodes.Status400BadRequest
                });
            }

            var orderHeader = await _orderRepository.Get(orderHeaderId.Value);
            if (orderHeader==null)
            {
                return BadRequest(new ErrorModelDTO()
                {
                    ErrorMessage="Invalid Id",
                    StatusCode=StatusCodes.Status404NotFound
                });
            }

            return Ok(orderHeader);
        }

        [HttpPost]
        [ActionName("Create")]
        public async Task<IActionResult> Create([FromBody] StripePaymentDTO paymentDTO)
        {
            paymentDTO.Order.OrderHeader.OrderDate=DateTime.Now;
            var result = await _orderRepository.Create(paymentDTO.Order);
            return Ok(result);  
        }


        [HttpPut]
        [ActionName("updatestatus")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] OrderStatusDTO orderHeader)
        {
            var result = await _orderRepository.UpdateOrderStatus(orderHeader.OrderId, orderHeader.Status);
            return Ok(result);
        }

        [HttpPost]
        [ActionName("paymentsuccessful")]
        public async Task<IActionResult> PaymentSuccessful([FromBody] OrderHeaderDTO orderHeaderDTO)
        {
            // payment will always success as COD :)

            //var service = new SessionService();
            //var sessionDetails = service.Get(orderHeaderDTO.SessionId);
            //if(sessionDetails.PaymentStatus =="paid")
            //{
                var result = await _orderRepository.MarkPaymentSuccessful(orderHeaderDTO.Id);
                //await _emailSender.SendEmailAsync(orderHeaderDTO.Email, "DD Order Confirmation",
                //    "New Order has been created :" + orderHeaderDTO.Id);
                if (result==null)
                {
                    return BadRequest(new ErrorModelDTO()
                    {
                        ErrorMessage = "Can not mark payment as successful"
                    });
                }
                return Ok(result);
            //}

            //return BadRequest();
        }
    }
}
