﻿using doan1_v1.Models;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using SQLitePCL;
using System.Diagnostics;

namespace doan1_v1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NTFashionDbContext _context;

        public HomeController(ILogger<HomeController> logger, NTFashionDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        [Route("")]
        public async Task<IActionResult> Index()
        {
            //viet de lay 8 san pham (4 quan, 4 ao)

            //ao
            var productAos = await _context.Products
                                            .Where(p => p.CategoryId == 4)
                                            .Include(p=> p.ProductImages)
                                            .Take(4)
                                            .ToListAsync();
            //quan
            var productQuans = await _context.Products
                                            .Where(p=> p.CategoryId ==5)
                                            .Include(p=> p.ProductImages)
                                            .Take(4)
                                            .ToListAsync();
            ViewBag.ProductAos = productAos;
            ViewBag.ProductQuans = productQuans;
            return View();
        }
        [Route("Shop/{id?}")]
        public async Task<IActionResult> Shop(int? id)
		{
			//danh muc san pham
			var categories = await _context.Categories
								.Where(c => c.ParentId != null)
								.ToListAsync();
			ViewBag.categories = categories;
			// lay tat ca san pham
			if (id == null)
            {
				var products = await _context.Products
					.Where(p => p.IsDel == false)
                    .Include(p=> p.ProductImages)
					.ToListAsync();
				ViewBag.products = products;
				return View();
            }
            //lay san pham theo category
            else
            {
                var products = await _context.Products
                                    .Where(p=>p.CategoryId == id && p.IsDel == false)
                                    .Include(p=> p.ProductImages)
                                    .ToListAsync();
                ViewBag.products = products;
                return View();
            }
			
		}
        [Route("Profile")]
        public IActionResult Profile()
		{
			return View();
		}
        [Route("Order")]
        public async Task<IActionResult> Order()
        {
            //lay id user = 5
            int customerId = 5;
			var orders = await _context.Orders
                         .Where(o=> o.CustomerId == 5)
						.Include(o => o.OrderProductDetails)
						.ToListAsync();
			ViewBag.Orders = orders;
			return View();
        }
		[Route("Invoice")]
		public async Task<IActionResult> Invoice(int orderId, string title)
		{
            //Console.WriteLine($"-------------------{orderId}---------------------");
            var order = await _context.Orders
                    .Where(o => o.Id == orderId)
                    .Include(o => o.Customer)
                    .Include(o => o.OrderProductDetails) // lay danh sach chi tiet order trong order
                        .ThenInclude(op => op.Product) // lay thong tin cua product trong danh sach chi tiet
                            .ThenInclude(opc => opc.Category) // lay ten category trong product
                    .FirstOrDefaultAsync();
            if (order != null)
            {
                ViewBag.Invoice = order;
                ViewData["title"] = title;
            }
            
			return View();
		}
		[Route("product/detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
		{
            // san pham chinh
            var product = await _context.Products.
                           Where(p => p.Id == id)
                           .Include (p => p.ProductImages)
                           .Include(p=> p.Category)
						   .FirstOrDefaultAsync();
            ViewBag.product = product;
            //san pham tuong tu
            var relativeProducts = await _context.Products
                                    .Where(p=> p.CategoryId == product.CategoryId)
                                    .Include(p=> p.ProductImages)
                                    .Include(p=>p.Category)
                                    .Take(3)
                                    .ToListAsync();
            ViewBag.relativeProducts = relativeProducts;

            //danh muc san pham
            var categories = await _context.Categories
                                .Where(c => c.ParentId != null)
                                .ToListAsync();
            ViewBag.categories = categories;
			return View();
		}

        [Route("Contact")]
        public IActionResult Contact()
		{
			return View();
		}
        [Route("Checkout")]
        public async Task<IActionResult> Checkout(int cartId)
		{

			//tìm chi tiết sản phẩm
			var details = await _context.CartDetails
										.Where(d => d.CartId == cartId)
										.Include(d => d.Product)
										.ThenInclude(p => p.ProductImages)
										.ToListAsync();
			if (details.Any())
			{

				//tính tổng tiền của giỏ hàng
				double totalPrice = 0;
				foreach (var detail in details)
				{
					int quantity = detail.Quantity;
					double price = (double)detail.Product.Price;
					totalPrice += quantity * price;
				}
				ViewBag.details = details; //chi tiet gio hang
				ViewBag.totalPrice = totalPrice; // tong tien gio hang
                var cart = await _context.Carts.Where(c => c.Id == cartId).Include(p=>p.User).FirstOrDefaultAsync();
                ViewBag.cart = cart;
			}

			return View();
		}
        //hàm dùng xác nhận đơn hàng
        public async Task<IActionResult> confirmCheckout(int cartId, double deliveryCost, int customerId)
        {
            //khi đặt hàng thì chuyển chi tiết giỏ hàng vào chi tiết order và xóa hết chi tiết giỏ hàng của cart đó
            //Console.WriteLine($"Vao post roi");
            //Console.WriteLine($"---------------------{customerId}------------------");
			//tao order
            Order order = new Order
			{
				Status = "Đã đặt hàng" /* gán giá trị phù hợp cho Status */
			};
            order.DateOrder = DateOnly.FromDateTime(DateTime.Now);
            order.DeliveryCost = deliveryCost;
            order.CustomerId = customerId;

            _context.Add(order);
            int rowsEffect = await _context.SaveChangesAsync();

            if(rowsEffect > 0) {
                //tao chi tiet order
                int orderId = order.Id;
                //tim cart de chuyen du lieu
                ////tim chi tiet cart dua vao cartId
                var detailCarts = await _context.CartDetails
                                        .Where(d => d.CartId == cartId)
                                        .Include(d=>d.Product)
                                        .ToListAsync();

                ////chuyen tat ca cac chi tiet vao chi tiet order
                foreach(var detailCart in detailCarts)
                {
                    OrderProductDetail detailOder = new OrderProductDetail();
                    detailOder.Quantity = detailCart.Quantity;
                    detailOder.PriceSale = (double)detailCart.Product.Price;
                    detailOder.ProductId = detailCart.ProductId;
                    detailOder.OrderId = orderId;
                    _context.Add(detailOder);
                    await _context.SaveChangesAsync();
                }
                ////xoa tat ca cac chi tiet trong chi tiet cart
                foreach(var detailCart in detailCarts)
                {
                    _context.CartDetails.Remove(detailCart);
                    await _context.SaveChangesAsync();
                }
			}



			// về trang hiện tại (không di chuyển qua trang khác)
			return Redirect("Oder");
		}
			[Route("Cart")]
        public async Task<IActionResult> Cart(int? userId)
        {
            //tim cart theo ten nguoi dung - id test nguoi dung la 5
            var cart = await _context.Carts
                                    .Where(c => c.UserId == 5)
                                    .FirstOrDefaultAsync();
            if (cart != null)
			{
                //tìm chi tiết sản phẩm
				var details = await _context.CartDetails
                                            .Where(d => d.CartId == cart.Id)
                                            .Include(d => d.Product)
                                            .ThenInclude(p => p.ProductImages)
                                            .ToListAsync();
                if (details.Any())
                {
                    
                    //tính tổng tiền của giỏ hàng
                    double totalPrice = 0;
                    foreach(var detail in details)
                    {
                        int quantity = detail.Quantity;
                        double price = (double)detail.Product.Price;
                        totalPrice += quantity * price;
                    }
					ViewBag.details = details; //chi tiet gio hang
					ViewBag.totalPrice = totalPrice; // tong tien gio hang
                }
                else
                {
                    ViewBag.details = null;
                }
			}
            else
            {
				ViewBag.details = null;
				
			}
			return View();

		}

        // hàm thêm vào giỏ hàng cần số lượng sản phẩm, id sản phẩm, id của user đã đăng nhập
		public async Task<IActionResult> addToCart(int quantity, int productId, int userId)
		{
			//tạo chi tiết giỏ hàng

			//đã có chi tiết giỏ hàng
            
            //tìm kiếm cart của một khách hàng dựa vào userId
            var cart = await _context.Carts
                            .Where(p => p.UserId == userId)
                            .FirstOrDefaultAsync();
            //tạo một chi tiết giỏ hàng
            CartDetail detail = new CartDetail();
            detail.ProductId = productId;
            detail.Quantity = quantity;
            detail.CartId = cart.Id;
            _context.Add(detail);
            int rowsEffect = await _context.SaveChangesAsync();
            //if(rowsEffect > 0)
            //{
            //    Console.WriteLine("Da tao cartdetail");
            //}
            //else
            //{
            //    Console.WriteLine("loi roi");
            //}

            // về trang hiện tại (không di chuyển qua trang khác)
			return Redirect(Request.Headers["Referer"].ToString());
		}

		//hàm xóa chi tiết trong giỏ hàng, nhận vào carid, id cua chi tiet
		public async Task<IActionResult> delInCart(int detailId, int cartId)
        {

            //Console.WriteLine($"------------------{detailId} {cartId}--------------------");
            
            //tìm cartdetail dựa vào cartId và detailId
            var detailCart = await _context.CartDetails
                                            .Where(d=> d.CartId == cartId && d.Id == detailId)
                                            .FirstOrDefaultAsync();

            if(detailCart != null)
            {
                //Console.WriteLine("----------------co san pham------------------");
                _context.CartDetails.Remove(detailCart);
                //xoa ra khoi gio hang luon
                await _context.SaveChangesAsync();
            }
			// về trang hiện tại (không di chuyển qua trang khác)
			return Redirect(Request.Headers["Referer"].ToString());
		}

        //hàm chỉnh sửa số lượng sản phẩm trong giỏ hàng, nhận vào cartId, id của chi tiết và quantity
        public async Task<IActionResult> changeQuantity(int detailId, int cartId, int quantity)
        {
            //Console.WriteLine($"----------------{detailId} {cartId} {quantity}------------");
            //đã có các tham số rồi
            //tìm cartdetail dựa vào cartId và detailId
            var detailCart = await _context.CartDetails
                                            .Where(d => d.CartId == cartId && d.Id == detailId)
                                            .FirstOrDefaultAsync();


            if (detailCart != null)
            {
                detailCart.Quantity = quantity;
                _context.Update(detailCart);
				await _context.SaveChangesAsync();
			}

            // về trang hiện tại (không di chuyển qua trang khác)
            return Redirect(Request.Headers["Referer"].ToString());
		}
		[Route("SignIn")]
        public IActionResult SignIn()
		{
			return View();
		}
        [Route("SignUp")]
        public IActionResult SignUp()
        {

			return View();
        }
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SignUp(Customer customer)
		{
			//khi tạo tài khoản thành công thì tạo một cart luôn
			Cart cart = new Cart();
			    cart.UserId = 0; // id cua nguoi dung
			    _context.Add(cart);
			    await _context.SaveChangesAsync();
            return RedirectToAction("Index");
		}
		[Route("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
