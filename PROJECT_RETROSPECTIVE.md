# Project Retrospective

## 🌟 What Went Well
- **Part 4 Delivery**: The Notification and IoT systems were integrated faster than expected thanks to the solid service-oriented architecture established in Parts 1-3.
- **Migration Strategy**: The hybrid `DbMigrationHelper` approach successfully solved the "Existing Data" vs "Fresh Install" dilemma without data loss.
- **Real-time Features**: SignalR implementation proved to be straightforward and effective for instant updates.

## 🚧 Challenges Faced
- **Database Schema Conflicts**: We encountered pre-existing tables (`AspNetUsers`) that conflicted with EF Core's initial migration.
  - *Solution*: Implemented a manual SQL check and conditional migration execution.
- **Missing Navigation Properties**: Some early Entity definitions lacked inverse navigation properties (e.g., `IoTSensor.Data`), causing LINQ errors.
  - *Solution*: Refactored Entities and re-ran migrations.

## 🎓 Lessons Learned
- **Tests First**: Writing Controller tests earlier would have caught the navigation property issues sooner (Fail Fast).
- **Documentation**: Keeping `API_DOCUMENTATION.md` updated in real-time is crucial for Frontend-Backend alignment.

## 🚀 Future Improvements
1. **Redis Caching**: Implement caching for `GET /courses` and `GET /sensors` to reduce DB load.
2. **Background Jobs**: Use **Hangfire** for scheduled tasks (automated daily attendance reports via email).
3. **CI/CD**: Set up GitHub Actions to run the Test Suite automatically on Push.
