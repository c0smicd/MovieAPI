-- ============================================================
-- MovieAPI Seed Data
-- Run AFTER: dotnet ef database update
-- Usage:     mysql -u <user> -p MovieAPI < seed.sql
-- ============================================================

USE MovieAPI;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE AuditoriumMovie;
TRUNCATE TABLE Auditoriums;
TRUNCATE TABLE SeatingPlans;
TRUNCATE TABLE Movies;
TRUNCATE TABLE IdempotencyRecords;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- SEATING PLANS
-- ============================================================

INSERT INTO SeatingPlans (Id, PlanName, Description, LayoutJson) VALUES
(1, 'Standard',       'Standardsaal mit 120 Sitzen in 12 Reihen',
 '{"rows":12,"seatsPerRow":10,"vipRows":[],"accessibleSeats":[{"row":1,"seat":1},{"row":1,"seat":10}]}'),

(2, 'Premium',        'Premiumsaal mit breiten Sitzen und viel Beinfreiheit (80 Sitze)',
 '{"rows":8,"seatsPerRow":10,"vipRows":[1,2],"accessibleSeats":[{"row":1,"seat":1}]}'),

(3, 'IMAX',           'IMAX-Saal mit 200 Sitzen auf gewölbter Leinwand',
 '{"rows":20,"seatsPerRow":10,"vipRows":[],"accessibleSeats":[{"row":1,"seat":1},{"row":1,"seat":10},{"row":20,"seat":1},{"row":20,"seat":10}]}'),

(4, 'Kleinsaal',      'Kleiner Saal für Previews und Events (48 Sitze)',
 '{"rows":6,"seatsPerRow":8,"vipRows":[],"accessibleSeats":[{"row":1,"seat":1}]}');

-- ============================================================
-- AUDITORIUMS
-- ============================================================

INSERT INTO Auditoriums (Id, AuditoriumName, SeatingPlanId) VALUES
(1, 'Saal 1',       1),
(2, 'Saal 2',       1),
(3, 'Saal 3 VIP',   2),
(4, 'IMAX Saal',    3),
(5, 'Vorschausaal', 4);

-- ============================================================
-- MOVIES
-- ============================================================

INSERT INTO Movies (Id, Title, Description, Genre, Director, Year, RuntimeMinutes, Rating, PosterUrl, CreatedAt, UpdatedAt) VALUES
(1,  'Inception',
     'Ein Dieb, der in die Träume anderer eindringt, bekommt den Auftrag, eine Idee zu pflanzen.',
     'Sci-Fi, Thriller', 'Christopher Nolan', 2010, 148, 8.8,
     'https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg',
     '2024-01-01 10:00:00', NULL),

(2,  'The Dark Knight',
     'Batman muss sich dem Chaos stellen, das der Joker in Gotham City verbreitet.',
     'Action, Crime', 'Christopher Nolan', 2008, 152, 9.0,
     'https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg',
     '2024-01-01 10:00:00', NULL),

(3,  'Interstellar',
     'Eine Gruppe von Astronauten reist durch ein Wurmloch auf der Suche nach einem neuen Heimatplaneten.',
     'Sci-Fi, Drama', 'Christopher Nolan', 2014, 169, 8.7,
     'https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg',
     '2024-01-02 10:00:00', NULL),

(4,  'The Matrix',
     'Ein Computerhacker entdeckt, dass die Realität eine Simulation ist.',
     'Sci-Fi, Action', 'Lana Wachowski, Lilly Wachowski', 1999, 136, 8.7,
     'https://image.tmdb.org/t/p/w500/f89U3ADr1oiB1s9GkdPOEpXUk5H.jpg',
     '2024-01-03 10:00:00', NULL),

(5,  'Dune: Part Two',
     'Paul Atreides vereint sich mit den Fremen und plant Rache an den Verschwörern.',
     'Sci-Fi, Adventure', 'Denis Villeneuve', 2024, 166, 8.5,
     'https://image.tmdb.org/t/p/w500/1pdfLvkbY9ohJlCjQH2CZjjYVvJ.jpg',
     '2024-03-01 10:00:00', NULL),

(6,  'Oppenheimer',
     'Die Geschichte des amerikanischen Physikers J. Robert Oppenheimer und die Entwicklung der Atombombe.',
     'Biography, Drama, History', 'Christopher Nolan', 2023, 180, 8.6,
     'https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg',
     '2024-03-15 10:00:00', NULL),

(7,  'Avatar: The Way of Water',
     'Jake Sully und Ney''tiri müssen zusammen kämpfen, um ihre Familie und ihren Heimatplaneten zu schützen.',
     'Sci-Fi, Action, Adventure', 'James Cameron', 2022, 192, 7.6,
     'https://image.tmdb.org/t/p/w500/t6HIqrRAclMCA60NsSmeqe9RmNV.jpg',
     '2024-04-01 10:00:00', NULL),

(8,  'The Shawshank Redemption',
     'Zwei Männer freunden sich im Gefängnis an und finden Trost und letztlich Erlösung durch Güte.',
     'Drama', 'Frank Darabont', 1994, 142, 9.3,
     'https://image.tmdb.org/t/p/w500/lyQBXAljKlgDbc0MbuNKsz8Pq7h.jpg',
     '2024-04-10 10:00:00', NULL),

(9,  'Parasite',
     'Eine mittelloser Familie schleust sich in das Leben einer wohlhabenden Familie ein.',
     'Thriller, Drama', 'Bong Joon-ho', 2019, 132, 8.5,
     'https://image.tmdb.org/t/p/w500/7IiTTgloJzvGI1TAYymCfbfl3vT.jpg',
     '2024-04-20 10:00:00', NULL),

(10, 'Poor Things',
     'Die außergewöhnliche Geschichte von Bella Baxter, die zum Leben erweckt und von der Welt fasziniert ist.',
     'Comedy, Drama, Fantasy', 'Yorgos Lanthimos', 2023, 141, 8.0,
     'https://image.tmdb.org/t/p/w500/kCGlIMHnOm8JPXIocHkzwkAso1M.jpg',
     '2024-05-01 10:00:00', NULL);

-- ============================================================
-- AUDITORIUM <-> MOVIE  (many-to-many)
-- ============================================================

INSERT INTO AuditoriumMovie (AuditoriumsId, MoviesId) VALUES
-- Saal 1
(1, 1),  -- Inception
(1, 3),  -- Interstellar
(1, 6),  -- Oppenheimer
-- Saal 2 (gleicher Plan wie Saal 1)
(2, 2),  -- The Dark Knight
(2, 4),  -- The Matrix
(2, 8),  -- Shawshank Redemption
-- Saal 3 VIP
(3, 5),  -- Dune Part Two
(3, 10), -- Poor Things
-- IMAX Saal
(4, 7),  -- Avatar
(4, 5),  -- Dune Part Two
(4, 3),  -- Interstellar
-- Vorschausaal
(5, 9),  -- Parasite
(5, 10); -- Poor Things
