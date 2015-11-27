﻿using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using LibraryManagementSystem.DAL;
using LibraryManagementSystem.DAL.Interfaces;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Models.ViewModels;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly LibraryDataContext _db = new LibraryDataContext();
        private readonly IReservationRepository _reservationRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILibraryItemRepository _libraryItemRepository;

        public ReservationsController(IReservationRepository reservationRepository, 
            ICustomerRepository customerRepository, ILibraryItemRepository libraryItemRepository)
        {
            this._reservationRepository = reservationRepository;
            this._customerRepository = customerRepository;
            this._libraryItemRepository = libraryItemRepository;
        }


        // GET: Reservations
        public ActionResult Index()
        {
            return View(_db.Reservations.ToList());
        }

        // GET: Reservations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = _db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        // GET: Reservations/Create
        public ActionResult Create([Bind(Include = "CustomerNumber,ItemId")]AddReservationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                /*db.Entry(reservation).State = EntityState.Modified;
                db.SaveChanges();*/
                //return RedirectToAction("Index");
            }
            /*if (customerNumber != null)
            {
                ViewBag.customer = Customer.FindByCustomerNumber(customerNumber);
            }*/

            return View();
        }

        [HttpPost]
        public JsonResult AjaxCreate()
        {
            int customerId;
            int itemId;
            bool isReserved;

            try
            {
                customerId = int.Parse(Request.Form["CustomerId"]);
                itemId = int.Parse(Request.Form["LibraryItemId"]);
                isReserved = bool.Parse(Request.Form["IsReserved"]);
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    response = "Oops! Something went wrong. Please refresh and try again."
                });
            }

            var customer = _customerRepository.FindBy(s => s.Id == customerId).Include(s => s.Reservations).FirstOrDefault();
            var item = _libraryItemRepository.Find(itemId);

            if (customer == null)
            {
                return Json(new
                {
                    status = false,
                    response = "Customer does not exist."
                });
            }

            if (item == null)
            {
                return Json(new
                {
                    status = false,
                    response = "Item does not exist."
                });
            }

            if (!item.CanCheckOut)
            {
                return Json(new
                {
                    status = false,
                    response = $"This item is a <b>{item.ItemType}</b>, which is not allowed to be checked out."
                });
            }
            
            if (_reservationRepository.FindBy(i => i.LibraryItem_Id == item.Id)
                                        .FirstOrDefault(c => c.Customer_Id == customer.Id) != null)
            {
                return Json(new
                {
                    status = false,
                    response = "This custom has already checked out this item."
                });
            }

            if (customer.Reservations.Count >= 5)
            {
                return Json(new
                {
                    status = false,
                    response = "Maximum checkout/reservation limit reached. Please return " +
                               "an item or cancel a reservation before attempting to check out another item."
                });
            }

            if (item.Quantity - _reservationRepository.CountItemReservations(item) == 0)
            {
                return Json(new
                {
                    status = false,
                    response = "There aren't any more copies of that item available."
                });
            }

            var reservation = new Reservation
            {
                CheckOutDate = DateTime.Today,
                Customer_Id =  customer.Id,
                LibraryItem_Id = item.Id
            };

            _reservationRepository.Add(reservation);
            _reservationRepository.Save();
            _reservationRepository.ReloadRepository(reservation);
            
            var action = isReserved ? "check it out" : "return it";
            var responseMessage = $"Item Reserved. You must {action} by {reservation.GetDueDate()}";

            return Json(new
            {
                status = true,
                response = new
                {
                    reservation = new
                    {
                        reservation.Id,
                        DueDate = reservation.GetDueDate(),
                        reservation.IsReserved
                    },
                    item = new
                    {
                       item.Title
                    },
                    message = responseMessage
                }
            });
        }

        [HttpPost]
        public JsonResult Details()
        {
            Int32 id;

            try
            {
                id = Int32.Parse(Request.Form["reservation_id"]);
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    response = "Oops! Something went wrong. Please refresh and try again."
                });
            }

            var reservation = _reservationRepository.FindBy(i => i.Id == id)
                                                        .Select(e => new
                                                        {
                                                            e,
                                                            ItemId = e.LibraryItem.Id
                                                        })
                                                        .FirstOrDefault();

            if (reservation == null)
            {
                return Json(new
                {
                    status = false,
                    response = "Oops! Something went wrong. Please refresh and try again."
                });
            }

            var reservedItem = _libraryItemRepository.Find(reservation.ItemId);

            return Json(new
            {
                status = true,
                response = new
                {
                    id = reservation.e.Id,
                    checkOutDate = reservation.e.FormatCheckOutDate(),
                    dueDate = reservation.e.GetDueDate(),
                    lateFee = reservation.e.CalculateLateFee(),
                    item = new
                    {
                        id = reservedItem.Id,
                        title = reservedItem.Title,
                        type = reservedItem.GetItemType()
                    }
                }
            });
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CustomerId,IsReserved")] Reservation reservation, string customerNumber)
        {
            if (customerNumber.Length != 9)
            {
                ModelState.AddModelError("customerNumber","Invalid Customer Number");
            }

            if (ModelState.IsValid)
            {
                Response.Write(Request.Form + "<br />");
                Response.Write("---->" + customerNumber);
                //db.Reservations.Add(reservation);
                //db.SaveChanges();
                //return RedirectToAction("Index");
            }

            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = _db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,IsReserved,CheckOutDate")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(reservation).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = _db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        [HttpPost]
        public JsonResult AjaxDelete()
        {
            try
            {
                var id = int.Parse(Request.Form["id"]);
                var reservation = _reservationRepository.Find(id);
                _reservationRepository.Delete(reservation);
                _reservationRepository.Save();

                return Json(new
                {
                    status = true,
                    response = "Item successfully checked in."
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    response = "Oops! Something went wrong. Please refresh and try again."
                });
            }
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Reservation reservation = _db.Reservations.Find(id);
            _db.Reservations.Remove(reservation);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
