// ADDS Project Service
using System;
using System.Data;
using ADDS.DataAccess;

namespace ADDS.Services
{
    public class ProjectService
    {
        private readonly IStoredProcedureRunner _runner;

        // Constructor injection – enables unit testing without a live Oracle
