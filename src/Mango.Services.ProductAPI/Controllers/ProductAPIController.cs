using AutoMapper;
using Mango.Services.CouponAPI.Models.DTO;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ProductAPI.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private ResponseDTO _response;
        private IMapper _mapper;

        public ProductAPIController(IMapper mapper, AppDbContext db)
        {
            _response = new ();
            _mapper = mapper;
            _db = db;
        }

        [HttpGet]
        //[Authorize]
        public ResponseDTO Get()
        {
            try
            {
                IEnumerable<Product> objList = _db.Product.ToList();
                //_response.Result = objList;
                _response.Result = _mapper.Map<IEnumerable<ProductDto>>(objList);
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
        [HttpGet]
        [Route("{id:int}")]
        [Authorize]
        public ResponseDTO Get(int id)
        {
            try
            {
                Product? Obj = _db.Product.FirstOrDefault(p => p.ProductId == id);

                //_response.Result = Obj;
                _response.Result = _mapper.Map<ProductDto>(Obj);

            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

     

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public ResponseDTO Post(ProductDto ProductDto)
        {
            try
            {
                Product product = _mapper.Map<Product>(ProductDto);
                _db.Product.Add(product);
                _db.SaveChanges();

				if (ProductDto.Image != null)
				{

					string fileName = product.ProductId + Path.GetExtension(ProductDto.Image.FileName);
					string filePath = @"wwwroot\ProductImages\" + fileName;

					//I have added the if condition to remove the any image with same name if that exist in the folder by any change
					var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);
					FileInfo file = new FileInfo(directoryLocation);
					if (file.Exists)
					{
						file.Delete();
					}

					var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
					using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
					{
						ProductDto.Image.CopyTo(fileStream);
					}
					var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
					product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
					product.ImageLocalPathUrl = filePath;
				}
				else
				{
					product.ImageUrl = "https://placehold.co/600x400";
				}
				_db.Product.Update(product);
				_db.SaveChanges();
				_response.Result = _mapper.Map<Product>(product);
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPut]
        [Authorize(Roles = "ADMIN")]
        public ResponseDTO put(ProductDto productDTO)
        {
            try
            {
                Product product = _mapper.Map<Product>(productDTO);

				if (productDTO.Image != null)
				{
					if (!string.IsNullOrEmpty(product.ImageLocalPathUrl))
					{
						var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPathUrl);
						FileInfo file = new FileInfo(oldFilePathDirectory);
						if (file.Exists)
						{
							file.Delete();
						}
					}

					string fileName = product.ProductId + Path.GetExtension(productDTO.Image.FileName);
					string filePath = @"wwwroot\ProductImages\" + fileName;
					var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
					using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
					{
						productDTO.Image.CopyTo(fileStream);
					}
					var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
					product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
					product.ImageLocalPathUrl = filePath;
				}

				_db.Product.Update(product);
                _db.SaveChanges();
                _response.Result = _mapper.Map<Product>(product);
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpDelete]
        [Route("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public ResponseDTO Delete(int id)
        {
            try
            {
                Product? Obj = _db.Product.FirstOrDefault(p => p.ProductId == id);
				if (!string.IsNullOrEmpty(Obj.ImageLocalPathUrl))
				{
					var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), Obj.ImageLocalPathUrl);
					FileInfo file = new FileInfo(oldFilePathDirectory);
					if (file.Exists)
					{
						file.Delete();
					}
				}
				_db.Product.Remove(Obj);
                _db.SaveChanges();
                _response.IsSucess = true;
                _response.Message = "Record Deleted Successfuly";
            }
            catch (Exception ex)
            {
                _response.IsSucess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
    }
}

