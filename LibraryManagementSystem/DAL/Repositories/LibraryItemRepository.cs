﻿
using LibraryManagementSystem.DAL.Interfaces;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.DAL.Repositories
{
    public class LibraryItemRepository : GenericRepository<LibraryDataContext, LibraryItem>, ILibraryItemRepository
    {
    }
}