﻿using AutoMapper;
using Mango.MessageBus;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Models.DTO;
using Mango.Services.ShoppingCartAPI.RabbitMQSender;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.PortableExecutable;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private ResponseDTO _response;
        private IMapper _mapper;
        private IProductService _productService;
        private ICouponService _couponService;
        private IMessageBus _messageBus;
        private IConfiguration _configuration;
        private readonly IRabbitMQCartMessageSender _messageSender;
        public CartAPIController(IMapper mapper, AppDbContext db, IProductService productService, ICouponService couponService, IMessageBus messageBus, IConfiguration configuration, IRabbitMQCartMessageSender messageSender)
        {
            _response = new();
            _mapper = mapper;
            _db = db;
            _productService = productService;
            _couponService = couponService;
            _messageBus = messageBus;
            _configuration = configuration;
            _messageSender = messageSender;
        }


        [HttpGet("GetCart/{userId}")]
        public async Task<ResponseDTO> GetCart(string userId)
        {
            try
            {
                CartDto cart = new()
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(_db.CartHeader.First(p => p.UserId == userId))

                };
                
                cart.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(_db.CartDetails.Where(p => p.CartHeaderId == cart.CartHeader.CartHeaderId));

                IEnumerable<ProductDto> productDtos = await _productService.GetProducts();

                
                foreach (var item in cart.CartDetails)
                {
                    item.Product=productDtos.FirstOrDefault(p => p.ProductId== item.ProductId);
                    cart.CartHeader.CartTotal += (item.Count * item.Product.Price);
                }

                //Apply coupon
                if(!string.IsNullOrEmpty(cart.CartHeader.CouponCode))
                {
                    CouponDTO Coupon  = await _couponService.GetCouponAsync(cart.CartHeader.CouponCode);
                    if(Coupon != null && cart.CartHeader.CartTotal > Coupon.MinAmount) 
                    {
                        cart.CartHeader.CartTotal -= Coupon.DiscountAmount;
                        cart.CartHeader.Discount = Coupon.DiscountAmount;
                    }
                }

                _response.Result = cart;
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }


        [HttpPost("ApplyCoupon")]
        public async Task<object> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeader.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
                _db.CartHeader.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.ToString();
            }
            return _response;
        }

        [HttpPost("RemoveCoupon")]
        public async Task<object> RemoveCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeader.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = "";
                _db.CartHeader.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.ToString();
            }
            return _response;
        }

        [HttpPost("EmailCartRequest")]
        public async Task<object> EmailCartRequest([FromBody] CartDto cartDto)
        {
            try
            {
                await _messageBus.PublishMessage(cartDto, _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue"));
                 _messageSender.SendMessage(cartDto, _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue"));
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.ToString();
            }
            return _response;
        }

            [HttpPost("CartUpSert")]
        public async Task<ResponseDTO> CartUpSert(CartDto cartDto)
        {
            try
            {
                var cartHeaderFromDB = await _db.CartHeader.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == cartDto.CartHeader.UserId);
                if (cartHeaderFromDB == null)
                {
                    //Create New Cartheader and details
                    CartHeader cartHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    _db.CartHeader.AddAsync(cartHeader);
                    await _db.SaveChangesAsync();
                    cartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;
                    _db.CartDetails.AddAsync(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    await _db.SaveChangesAsync();
                }
                else
                {
                    //retrive cartDeatils against the header and check if already have same product
                    var CartDetailsFromDB = await _db.CartDetails.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == cartDto.CartDetails.First().ProductId
                    && p.CartHeaderId == cartHeaderFromDB.CartHeaderId);
                    if (CartDetailsFromDB == null)
                    {
                        // if cart details is null means no product against this ID in cart we need to add new product new entry
                        cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDB.CartHeaderId;
                        _db.CartDetails.AddAsync(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        //means already have and update exisiting product
                        cartDto.CartDetails.First().Count += CartDetailsFromDB.Count;
                        cartDto.CartDetails.First().CartHeaderId = CartDetailsFromDB.CartHeaderId;
                        cartDto.CartDetails.First().CartDetailsId = CartDetailsFromDB.CartDetailsId;
                        _db.CartDetails.Update(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();

                    }
                    _response.Result = cartDto;
                }
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message;
                _response.IsSucess = false;
            }
            return _response;
        }

        [HttpPost("RemoveCart")]
        public async Task<ResponseDTO> RemoveCart([FromBody] int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = _db.CartDetails
                   .First(u => u.CartDetailsId == cartDetailsId);

                int totalCountofCartItem = _db.CartDetails.Where(u => u.CartHeaderId == cartDetails.CartHeaderId).Count();
                _db.CartDetails.Remove(cartDetails);
                if (totalCountofCartItem == 1)
                {
                    var cartHeaderToRemove = await _db.CartHeader.FirstOrDefaultAsync(u => u.CartHeaderId == cartDetails.CartHeaderId);

                    _db.CartHeader.Remove(cartHeaderToRemove);
                }
                await _db.SaveChangesAsync();

                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.Message = ex.Message.ToString();
                _response.IsSucess = false;
            }
            return _response;
        }
    }
}
