﻿// Copyright © 2010 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRadar.Interface.Database
{
    /// <summary>
    /// The interface for objects that can deal with the BaseStation database file for us.
    /// </summary>
    /// <remarks><para>
    /// The BaseStation database is an SQLite file that Kinetic's BaseStation application creates and maintains. By default the object implementing
    /// the interface is in read-only mode, it will not make any changes to the database. In this mode attempts to use the insert / update or delete
    /// methods should throw an InvalidOperation exception. If the program sets <see cref="WriteSupportEnabled"/> then the insert / update and delete
    /// methods should allow writes to the database.
    /// </para><para>
    /// Virtual Radar Server never sets <see cref="WriteSupportEnabled"/>, it will never write to the database. The write methods are only there for
    /// the use of plugins.
    /// </para></remarks>
    public interface IBaseStationDatabase : ITransactionable, IDisposable
    {
        /// <summary>
        /// Gets or sets the object that abstracts away the environment to help when testing.
        /// </summary>
        IBaseStationDatabaseProvider Provider { get; set; }

        /// <summary>
        /// Gets or sets the full path and filename of the database. Changing the filename causes the current connection
        /// to close, the next operation on the database causes it to open a new connection as per usual.
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full path and filename of the log.
        /// </summary>
        /// <remarks>
        /// This is not used by the application as it has an impact on performance but some plugins may use it to trace
        /// database calls. The implementation may leave the file open and locked while the object is alive. Logging may
        /// not be supported by all implementations.
        /// </remarks>
        string LogFileName { get; set; }

        /// <summary>
        /// Gets a value indicating that there is an open connection to the database.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns true if a connection could be made to <see cref="FileName"/>, false if it could not.
        /// If it could be made then the connection is left open.
        /// </summary>
        /// <returns></returns>
        bool TestConnection();

        /// <summary>
        /// Gets or sets a flag indicating that methods that can create or modify the database are enabled. By default
        /// this setting is disabled. Changing this setting closes the current connection, the next call to access the
        /// database will reopen it.
        /// </summary>
        bool WriteSupportEnabled { get; set; }

        /// <summary>
        /// Raised before <see cref="FileName"/> is changed as the result of a configuration change.
        /// </summary>
        event EventHandler FileNameChanging;

        /// <summary>
        /// Raised after <see cref="FileName"/> has changed as the result of a configuration change.
        /// </summary>
        event EventHandler FileNameChanged;

        /// <summary>
        /// If the database file is missing or entirely empty then this method creates the file and pre-populates the
        /// tables with roughly the same records that BaseStation prepopulates a new database with.
        /// </summary>
        /// <param name="fileName">The name of the database file to create. This need not be the same as <see cref="FileName"/>.</param>
        /// <remarks>
        /// This does nothing if the database file exists and is not zero-length or if the database file is not set.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="WriteSupportEnabled"/> is false.</exception>
        void CreateDatabaseIfMissing(string fileName);

        /// <summary>
        /// Returns the first aircraft with the registration passed across.
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        BaseStationAircraft GetAircraftByRegistration(string registration);

        /// <summary>
        /// Returns the first aircraft with the ICAO24 code passed across.
        /// </summary>
        /// <param name="icao24"></param>
        /// <returns></returns>
        BaseStationAircraft GetAircraftByCode(string icao24);

        /// <summary>
        /// Returns a list of every flight, or a subset of every flight, that matches the criteria passed across.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="criteria"></param>
        /// <param name="fromRow"></param>
        /// <param name="toRow"></param>
        /// <param name="sort1"></param>
        /// <param name="sort1Ascending"></param>
        /// <param name="sort2"></param>
        /// <param name="sort2Ascending"></param>
        /// <returns></returns>
        List<BaseStationFlight> GetFlightsForAircraft(BaseStationAircraft aircraft, SearchBaseStationCriteria criteria, int fromRow, int toRow, string sort1, bool sort1Ascending, string sort2, bool sort2Ascending);

        /// <summary>
        /// Returns the number of flight records that match the criteria passed across.
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="criteria"></param>
        /// <returns></returns>
        int GetCountOfFlightsForAircraft(BaseStationAircraft aircraft, SearchBaseStationCriteria criteria);

        /// <summary>
        /// Returns all flights, or a subset of all flights, that match the criteria passed across.
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="fromRow"></param>
        /// <param name="toRow"></param>
        /// <param name="sortField1"></param>
        /// <param name="sortField1Ascending"></param>
        /// <param name="sortField2"></param>
        /// <param name="sortField2Ascending"></param>
        /// <returns></returns>
        List<BaseStationFlight> GetFlights(SearchBaseStationCriteria criteria, int fromRow, int toRow, string sortField1, bool sortField1Ascending, string sortField2, bool sortField2Ascending);

        /// <summary>
        /// Returns the number of flights that match the criteria passed across.
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        int GetCountOfFlights(SearchBaseStationCriteria criteria);

        /// <summary>
        /// Returns all of the records from BaseStation's DBHistory table.
        /// </summary>
        /// <returns></returns>
        IList<BaseStationDBHistory> GetDatabaseHistory();

        /// <summary>
        /// Returns the single DBInfo record in BaseStation's DBInfo table. Note that this has no key.
        /// </summary>
        /// <returns></returns>
        BaseStationDBInfo GetDatabaseVersion();

        /// <summary>
        /// Returns the entire content of the SystemEvents table.
        /// </summary>
        /// <returns></returns>
        IList<BaseStationSystemEvents> GetSystemEvents();

        /// <summary>
        /// Inserts a new SystemEvents record and sets the SystemEventsID to the identifier of the new record.
        /// </summary>
        /// <param name="systemEvent"></param>
        void InsertSystemEvent(BaseStationSystemEvents systemEvent);

        /// <summary>
        /// Updates an existing SystemEvents record.
        /// </summary>
        /// <param name="systemEvent"></param>
        void UpdateSystemEvent(BaseStationSystemEvents systemEvent);

        /// <summary>
        /// Deletes an existing SystemEvents record.
        /// </summary>
        /// <param name="systemEvent"></param>
        void DeleteSystemEvent(BaseStationSystemEvents systemEvent);

        /// <summary>
        /// Returns all of the locations from BaseStation's Locations table.
        /// </summary>
        /// <returns></returns>
        IList<BaseStationLocation> GetLocations();

        /// <summary>
        /// Inserts a new record in the database for the location passed across and sets the LocationID to the
        /// identifier of the new record.
        /// </summary>
        /// <param name="location"></param>
        void InsertLocation(BaseStationLocation location);

        /// <summary>
        /// Updates an existing location record.
        /// </summary>
        /// <param name="location"></param>
        void UpdateLocation(BaseStationLocation location);

        /// <summary>
        /// Deletes an existing location record.
        /// </summary>
        /// <param name="location"></param>
        void DeleteLocation(BaseStationLocation location);

        /// <summary>
        /// Returns all of the sessions from BaseStation's Sessions table.
        /// </summary>
        /// <returns></returns>
        IList<BaseStationSession> GetSessions();

        /// <summary>
        /// Inserts a record in the Sessions table, setting SessionID to the identifier of the new record.
        /// </summary>
        /// <param name="session"></param>
        void InsertSession(BaseStationSession session);

        /// <summary>
        /// Updates the record for a session.
        /// </summary>
        /// <param name="session"></param>
        void UpdateSession(BaseStationSession session);

        /// <summary>
        /// Deletes the record for a session. This automatically deletes all flights associated with the session.
        /// </summary>
        /// <param name="session"></param>
        void DeleteSession(BaseStationSession session);

        /// <summary>
        /// Retrieves an aircraft record by its identifier.
        /// </summary>
        /// <param name="id"></param>
        BaseStationAircraft GetAircraftById(int id);

        /// <summary>
        /// Inserts a new aircraft record and fills AircraftID with the identifier of the record.
        /// </summary>
        /// <param name="aircraft"></param>
        void InsertAircraft(BaseStationAircraft aircraft);

        /// <summary>
        /// Updates an existing aircraft record.
        /// </summary>
        /// <param name="aircraft"></param>
        void UpdateAircraft(BaseStationAircraft aircraft);

        /// <summary>
        /// Updates the Mode-S country for an aircraft.
        /// </summary>
        /// <param name="aircraftId"></param>
        /// <param name="modeSCountry"></param>
        void UpdateAircraftModeSCountry(int aircraftId, string modeSCountry);

        /// <summary>
        /// Deletes an existing aircraft record.
        /// </summary>
        /// <param name="aircraft"></param>
        void DeleteAircraft(BaseStationAircraft aircraft);

        /// <summary>
        /// Retrieves a flight record from the database by its ID number. This does not read the associated aircraft record.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        BaseStationFlight GetFlightById(int id);

        /// <summary>
        /// Inserts a new flight record and assigns the unique identifier of the new record to the FlightID property. The AircraftID
        /// property must be filled with the identifier of an existing aircraft record.
        /// </summary>
        /// <param name="flight"></param>
        void InsertFlight(BaseStationFlight flight);

        /// <summary>
        /// Updates an existing flight record. Ignores the aircraft record attached to the flight (if any).
        /// </summary>
        /// <param name="flight"></param>
        void UpdateFlight(BaseStationFlight flight);

        /// <summary>
        /// Deletes an existing flight record. Ignores the aircraft record attached to the flight (if any).
        /// </summary>
        /// <param name="flight"></param>
        void DeleteFlight(BaseStationFlight flight);
    }
}
