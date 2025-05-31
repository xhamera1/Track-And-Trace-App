

-- by stworzenie bazy danych dziala poprawnie nalezy recznie wprowadzic w systemi uzytkownikow, 
-- poniewaz potrzebuja oni wygenerowania hasha hasla oraz api key, przez inserty sie nie da 



-- Usunięcie bazy danych, jeśli istnieje, aby zapewnić czysty start
DROP DATABASE IF EXISTS `trackandtrace`;

-- Utworzenie nowej bazy danych
CREATE DATABASE IF NOT EXISTS `trackandtrace`;

-- Wybór bazy danych do dalszych operacji
USE `trackandtrace`;

-- 1. Tabela Adresów
CREATE TABLE IF NOT EXISTS `Addresses` (
    `AddressId` INT AUTO_INCREMENT PRIMARY KEY,
    `Street` VARCHAR(255) NOT NULL,
    `City` VARCHAR(100) NOT NULL,
    `ZipCode` VARCHAR(10) NOT NULL,
    `Country` VARCHAR(100) NOT NULL,
    UNIQUE KEY `uq_address` (`Street`, `City`, `ZipCode`, `Country`)
);

-- 2. Tabela Użytkowników
CREATE TABLE IF NOT EXISTS `Users` (
    `UserId` INT AUTO_INCREMENT PRIMARY KEY,
    `Username` VARCHAR(32) UNIQUE NOT NULL,
    `Email` VARCHAR(128) UNIQUE NOT NULL,
    `Password` VARCHAR(255) NOT NULL,
    `ApiKey` CHAR(44) NULL,
    `FirstName` VARCHAR(100) NULL,
    `LastName` VARCHAR(100) NULL,
    `Role` ENUM('user', 'admin', 'courier') NOT NULL,
    `Birthday` DATE NULL,
    `AddressId` INT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`AddressId`) REFERENCES `Addresses`(`AddressId`)
);

-- 3. Tabela Definicji Statusów
CREATE TABLE IF NOT EXISTS `StatusDefinitions` (
    `StatusId` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(32) UNIQUE NOT NULL,
    `Description` VARCHAR(255) NOT NULL
);

-- 4. Tabela Paczek
CREATE TABLE IF NOT EXISTS `Packages` (
    `PackageId` INT AUTO_INCREMENT PRIMARY KEY,
    `TrackingNumber` VARCHAR(255) UNIQUE NOT NULL,
    `SenderUserId` INT NOT NULL,
    `RecipientUserId` INT NOT NULL,
    `AssignedCourierId` INT NULL,
    `PackageSize` VARCHAR(50) NOT NULL,
    `WeightInKg` DECIMAL(8, 2) NULL,
    `Notes` TEXT NULL,
    `OriginAddressId` INT NOT NULL,
    `DestinationAddressId` INT NOT NULL,
    `SubmissionDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `DeliveryDate` DATETIME NULL,
    `StatusId` INT NOT NULL,
    `Longitude` DECIMAL(10, 7) NULL,
    `Latitude` DECIMAL(10, 7) NULL,
    FOREIGN KEY (`SenderUserId`) REFERENCES `Users`(`UserId`),
    FOREIGN KEY (`RecipientUserId`) REFERENCES `Users`(`UserId`),
    FOREIGN KEY (`AssignedCourierId`) REFERENCES `Users`(`UserId`),
    FOREIGN KEY (`OriginAddressId`) REFERENCES `Addresses`(`AddressId`),
    FOREIGN KEY (`DestinationAddressId`) REFERENCES `Addresses`(`AddressId`),
    FOREIGN KEY (`StatusId`) REFERENCES `StatusDefinitions`(`StatusId`)
);

-- 5. Tabela Historii Paczek
CREATE TABLE IF NOT EXISTS `PackageHistory` (
    `PackageHistoryId` INT AUTO_INCREMENT PRIMARY KEY,
    `PackageId` INT NOT NULL,
    `StatusId` INT NOT NULL,
    `Timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Longitude` DECIMAL(10, 7) NULL,
    `Latitude` DECIMAL(10, 7) NULL,
    FOREIGN KEY (`PackageId`) REFERENCES `Packages`(`PackageId`),
    FOREIGN KEY (`StatusId`) REFERENCES `StatusDefinitions`(`StatusId`)
);

-- Indeksy dla tabeli Users
-- UserId (PRIMARY KEY) jest automatycznie indeksowany.
-- Username (UNIQUE) jest automatycznie indeksowany.
-- Email (UNIQUE) jest automatycznie indeksowany.
-- AddressId (FOREIGN KEY) powinien być indeksowany, ponieważ często będzie używany w JOINach z tabelą Addresses.
CREATE INDEX idx_users_addressid ON Users (AddressId);

-- Indeksy dla tabeli Packages
-- PackageId (PRIMARY KEY) jest automatycznie indeksowany.
-- TrackingNumber (UNIQUE) jest automatycznie indeksowany.
-- Klucze obce SenderUserId, RecipientUserId, AssignedCourierId
CREATE INDEX idx_packages_senderuserid ON Packages (SenderUserId);
CREATE INDEX idx_packages_recipientuserid ON Packages (RecipientUserId);
CREATE INDEX idx_packages_assignedcourierid ON Packages (AssignedCourierId);
CREATE INDEX idx_packages_originaddressid ON Packages (OriginAddressId);
CREATE INDEX idx_packages_destinationaddressid ON Packages (DestinationAddressId);
CREATE INDEX idx_packages_statusid ON Packages (StatusId);


-- Indeksy dla tabeli PackageHistory
-- PackageHistoryId (PRIMARY KEY) jest automatycznie indeksowany.
-- PackageId (FOREIGN KEY) jest kluczowy, ponieważ historia jest często wyszukiwana po ID paczki.
CREATE INDEX idx_packagehistory_packageid ON PackageHistory (PackageId);
CREATE INDEX idx_packagehistory_statusid ON PackageHistory (StatusId);

-- Insert Sample Addresses
INSERT IGNORE INTO `Addresses` (`Street`, `City`, `ZipCode`, `Country`) VALUES
('Rynek Główny 1', 'Kraków', '31-042', 'Polska'),
('ul. Piękna 5A', 'Warszawa', '00-500', 'Polska'),
('Aleje Jerozolimskie 100', 'Warszawa', '00-800', 'Polska'),
('ul. Sezamkowa 12', 'Gdańsk', '80-100', 'Polska');

-- Insert Sample Users
INSERT IGNORE INTO `Users` (`Username`, `Email`, `Password`, `ApiKey`, `FirstName`, `LastName`, `Role`, `Birthday`, `AddressId`) VALUES
('admin', 'admin@example.com', 'AQAAAAIAAYagAAAAENXNElv7OpwB8US60TqyEx5s6ZH+M2FWjH8W6MzJfs8k4pF1NfzACrMhXTSggOhWEg==', 'ABL+sYvvtiKRcMa4kDn/JhPyG4Q+Mme3BvgW7Y3HsGs=', 'Admin', 'Admin', 'admin', '1985-01-15', (SELECT AddressId FROM Addresses WHERE Street = 'Rynek Główny 1' AND City = 'Kraków')),
('jan_kowalski', 'jan.kowalski@example.com', 'AQAAAAIAAYagAAAAEMIK/OUZ6D47rr3EFZZhLeq9HgVuMb523ZypwdJjVbGu00GlUKznE/byUCZeq9Ejdw==', 'Mwc4lORruM40rwGU6hjY27G2wmWjk3rBbyOWg/1M1is=', 'Jan', 'Kowalski', 'user', '1990-03-20', (SELECT AddressId FROM Addresses WHERE Street = 'ul. Piękna 5A' AND City = 'Warszawa')),
('anna_limanowska', 'anna.limanowska@example.com', 'AQAAAAIAAYagAAAAEI1r7cAZaMnLTH3yCUTk1yGh6O5+NXaRys6HgV601qLP71BrlGxxLAsW3ZfICFd2Nw==', 'RsnlzH8zYnR3uYyJlnGO60d7aHGYkeau62ggBytIgDA=', 'Anna', 'Limanowska', 'user', '1992-07-10', (SELECT AddressId FROM Addresses WHERE Street = 'Aleje Jerozolimskie 100' AND City = 'Warszawa')),
('kurier_waw', 'kurier@example.com', 'AQAAAAIAAYagAAAAEA6YpkaV8cGpvYSiXHcZrji1/cfHDq18fLefHPD6evHgaL+S1bDS9JL73RjzMhgT4g==', '1jum3j8D8wsk0y0qLSPo3CMqrI0AP4lkZIBPIq3Kds4=', 'Karol', 'Kurierski', 'courier', '1998-12-12', (SELECT AddressId FROM Addresses WHERE Street = 'Aleje Jerozolimskie 100' AND City = 'Warszawa'));

-- Insert Status Definitions
INSERT IGNORE INTO `StatusDefinitions` (`Name`, `Description`) VALUES
('Sent', 'Paczka została zgłoszona do wysyłki.'),
('In Delivery', 'Kurier lub dostawca jest w trakcie doręczania paczki.'),
('Delivered', 'Paczka została pomyślnie dostarczona odbiorcy.');

-- Insert a Sample Package
-- Uwaga: W



-- Paczka 1: Aktywna, status "Sent", przypisana do kurier_waw
-- Nadawca: jan_kowalski (UserId=2), Adres nadania: ul. Piękna 5A, Warszawa (AddressId=2)
-- Odbiorca: anna_limanowska (UserId=3), Adres odbioru: ul. Sezamkowa 12, Gdańsk (AddressId=4)
INSERT INTO `Packages` (
    `TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`,
    `PackageSize`, `WeightInKg`, `Notes`,
    `OriginAddressId`, `DestinationAddressId`,
    `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`
) VALUES (
    'TT20250530001', 2, 3, 4,
    'Medium', 2.5, 'Ostrożnie, szkło.',
    2, 4,
    NOW(), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 19.9449799, 50.0614300 -- Przykładowa lokalizacja początkowa (Kraków)
);
SET @package1_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package1_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), NOW(), 19.9449799, 50.0614300);


-- Paczka 2: Aktywna, status "In Delivery", przypisana do kurier_waw
-- Nadawca: anna_limanowska (UserId=3), Adres nadania: Aleje Jerozolimskie 100, Warszawa (AddressId=3)
-- Odbiorca: jan_kowalski (UserId=2), Adres odbioru: ul. Piękna 5A, Warszawa (AddressId=2)
INSERT INTO `Packages` (
    `TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`,
    `PackageSize`, `WeightInKg`, `Notes`,
    `OriginAddressId`, `DestinationAddressId`,
    `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`
) VALUES (
    'TT20250530002', 3, 2, 4,
    'Small', 0.8, 'Dokumenty pilne.',
    3, 2,
    DATE_SUB(NOW(), INTERVAL 1 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 20.9923300, 52.2286900 -- Przykładowa lokalizacja (w drodze w Warszawie)
);
SET @package2_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`) VALUES
(@package2_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 2 HOUR)); -- Historia: najpierw była 'Sent'
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package2_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 1 HOUR), 20.9923300, 52.2286900);


-- Paczka 3: Już dostarczona, status "Delivered", przypisana do kurier_waw
-- Nadawca: jan_kowalski (UserId=2), Adres nadania: ul. Piękna 5A, Warszawa (AddressId=2)
-- Odbiorca: admin (UserId=1), Adres odbioru: Rynek Główny 1, Kraków (AddressId=1)
INSERT INTO `Packages` (
    `TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`,
    `PackageSize`, `WeightInKg`, `Notes`,
    `OriginAddressId`, `DestinationAddressId`,
    `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`
) VALUES (
    'TT20250529003', 2, 1, 4,
    'Large', 10.2, 'Sprzęt elektroniczny.',
    2, 1,
    DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 2 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), 19.9445440, 50.0618900 -- Lokalizacja dostarczenia (Kraków)
);
SET @package3_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`) VALUES
(@package3_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 1 DAY));
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package3_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 5 HOUR), 20.5, 51.5); -- Przykładowa lokalizacja w drodze
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package3_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), DATE_SUB(NOW(), INTERVAL 2 HOUR), 19.9445440, 50.0618900);


-- Paczka 4: Aktywna, status "In Delivery", przypisana do kurier_waw, inna trasa
-- Nadawca: anna_limanowska (UserId=3), Adres nadania: Aleje Jerozolimskie 100, Warszawa (AddressId=3)
-- Odbiorca: jan_kowalski (UserId=2), Adres odbioru: ul. Sezamkowa 12, Gdańsk (AddressId=4)
INSERT INTO `Packages` (
    `TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`,
    `PackageSize`, `WeightInKg`, `Notes`,
    `OriginAddressId`, `DestinationAddressId`,
    `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`
) VALUES (
    'TT20250530004', 3, 2, 4,
    'Medium', 3.0, 'Książki.',
    3, 4,
    DATE_SUB(NOW(), INTERVAL 30 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 18.6466384, 54.3520252 -- Lokalizacja (Gdańsk)
);
SET @package4_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`) VALUES
(@package4_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 1 HOUR));
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package4_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 30 MINUTE), 18.6466384, 54.3520252);


-- Paczka 5: Aktywna, status "Sent", przypisana do kurier_waw, oczekuje na podjęcie
-- Nadawca: admin (UserId=1), Adres nadania: Rynek Główny 1, Kraków (AddressId=1)
-- Odbiorca: anna_limanowska (UserId=3), Adres odbioru: Aleje Jerozolimskie 100, Warszawa (AddressId=3)
INSERT INTO `Packages` (
    `TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`,
    `PackageSize`, `WeightInKg`, `Notes`,
    `OriginAddressId`, `DestinationAddressId`,
    `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`
) VALUES (
    'TT20250530005', 1, 3, 4,
    'Small', 1.1, 'Ważne dokumenty.',
    1, 3,
    DATE_SUB(NOW(), INTERVAL 5 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 19.9449799, 50.0614300 -- Lokalizacja nadania (Kraków)
);
SET @package5_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package5_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 5 MINUTE), 19.9449799, 50.0614300);


-- Nowe dodania 

INSERT IGNORE INTO `Addresses` (`Street`, `City`, `ZipCode`, `Country`) VALUES
('ul. Floriańska 15', 'Kraków', '31-019', 'Polska'),                 -- ID: 5 (zakładając, że poprzednie kończyły się na 4)
('ul. Karmelicka 20', 'Kraków', '31-128', 'Polska'),                -- ID: 6
('ul. Długa 72', 'Kraków', '31-147', 'Polska'),                     -- ID: 7
('os. Złotej Jesieni 5/22', 'Kraków', '31-828', 'Polska'),          -- ID: 8
('ul. Wielicka 28', 'Kraków', '30-552', 'Polska'),                  -- ID: 9
('ul. Zakopiańska 62', 'Kraków', '30-418', 'Polska'),               -- ID: 10
('ul. Lubicz 4', 'Kraków', '31-034', 'Polska'),                     -- ID: 11
('ul. Starowiślna 30', 'Kraków', '31-032', 'Polska'),               -- ID: 12
('ul. Grzegórzecka 60', 'Kraków', '31-559', 'Polska'),              -- ID: 13
('ul. Lea 112', 'Kraków', '30-052', 'Polska'),                      -- ID: 14
('ul. Bronowicka 80', 'Kraków', '30-091', 'Polska'),                -- ID: 15
('Plac Inwalidów 6', 'Kraków', '30-033', 'Polska'),                 -- ID: 16
('ul. Szewska 2', 'Kraków', '31-009', 'Polska');                    -- ID: 17


---- odtad

INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531006', 5, 6, 8, 'Small', 0.5, 'Delikatne kwiaty.', 5, 6, DATE_SUB(NOW(), INTERVAL 3 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 19.938344, 50.063419); -- Kazimierz
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 4 HOUR), 19.937160, 50.061901), -- Floriańska
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 3 HOUR), 19.938344, 50.063419); -- Kazimierz (na trasie)

-- Paczka 7: Od Piotra (6) dla Magdy (7), kurier Tomasz (9), status "Delivered" (w Krakowie)
-- Origin: ul. Karmelicka 20 (ID 6), Dest: ul. Długa 72 (ID 7)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531007', 6, 7, 9, 'Medium', 1.8, 'Książki uniwersyteckie.', 6, 7, DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), 19.935390, 50.069910); -- Długa
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 2 DAY), 19.932900, 50.064910), -- Karmelicka
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 1 DAY), 19.935000, 50.067000), -- W drodze
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), DATE_SUB(NOW(), INTERVAL 1 DAY), 19.935390, 50.069910); -- Długa

-- Paczka 8: Od Magdy (7) dla Ewy (5), kurier Marek (8), status "Sent" (w Krakowie)
-- Origin: ul. Długa 72 (ID 7), Dest: ul. Floriańska 15 (ID 5)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531008', 7, 5, 8, 'Large', 5.2, 'Obraz.', 7, 5, DATE_SUB(NOW(), INTERVAL 30 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 19.935390, 50.069910); -- Długa
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.935390, 50.069910); -- Długa

-- Paczka 9: Od Kasi (10) dla admina_krakow (11), kurier Tomasz (9), status "In Delivery" (w Krakowie)
-- Origin: ul. Wielicka 28 (ID 9), Dest: ul. Lubicz 4 (ID 11)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531009', 10, 11, 9, 'Small', 0.3, 'Dokumenty firmowe.', 9, 11, DATE_SUB(NOW(), INTERVAL 1 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 19.952700, 50.060830); -- Grzegórzki (na trasie)
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 2 HOUR), 20.000850, 50.032150), -- Wielicka
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.952700, 50.060830); -- Grzegórzki

-- Paczka 10: Od admina (1) dla Kasi (10), kurier Marek (8), status "Sent" (z Krakowa do Krakowa)
-- Origin: Rynek Główny 1 (ID 1), Dest: ul. Zakopiańska 62 (ID 10)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531010', 1, 10, 8, 'Medium', 2.0, 'Prezent urodzinowy.', 1, 10, DATE_SUB(NOW(), INTERVAL 10 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 19.937000, 50.061400); -- Rynek Główny
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 10 MINUTE), 19.937000, 50.061400);

-- Paczka 11: Od Piotr (6) dla Admina_Krakow (11), kurier Tomasz (9), status "In Delivery" (w Krakowie)
-- Origin: ul. Karmelicka 20 (ID 6), Dest: os. Złotej Jesieni 5/22 (ID 8)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531011', 6, 11, 9, 'Small', 1.0, 'Gadżety firmowe.', 6, 8, DATE_SUB(NOW(), INTERVAL 2 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 20.015000, 50.070000); -- Czyżyny (na trasie)
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 3 HOUR), 19.932900, 50.064910), -- Karmelicka
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 2 HOUR), 20.015000, 50.070000); -- Czyżyny

-- Paczka 12: Od Anny (3 - Warszawa) dla Magdy (7 - Kraków), kurier Marek (8), status "In Delivery" (na trasie do Krakowa)
-- Origin: Aleje Jerozolimskie 100, Warszawa (ID 3), Dest: ul. Długa 72, Kraków (ID 7)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531012', 3, 7, 8, 'Medium', 3.5, 'Artykuły biurowe.', 3, 7, DATE_SUB(NOW(), INTERVAL 6 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 20.500000, 50.800000); -- Przykładowe miejsce na trasie WWA-KRK
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 7 HOUR), 21.012229, 52.229676), -- Warszawa
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 6 HOUR), 20.500000, 50.800000); -- Na trasie

-- Paczka 13: Od Jana (2 - Warszawa) dla Kasi (10 - Kraków), kurier Karol (4 - Warszawa), status "Sent" (oczekuje na transport z Warszawy)
-- Origin: ul. Piękna 5A, Warszawa (ID 2), Dest: ul. Lea 112, Kraków (ID 14)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531013', 2, 10, 4, 'Large', 7.1, 'Monitor komputerowy.', 2, 14, DATE_SUB(NOW(), INTERVAL 45 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 21.011000, 52.221000); -- Warszawa
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 45 MINUTE), 21.011000, 52.221000);

-- Paczka 14: Od Ewy (5 - Kraków) dla Admina (1 - Kraków), kurier Tomasz (9), status "Delivered"
-- Origin: ul. Floriańska 15 (ID 5), Dest: Rynek Główny 1 (ID 1)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531014', 5, 1, 9, 'Small', 0.2, 'Pendrive z danymi.', 5, 1, DATE_SUB(NOW(), INTERVAL 5 HOUR), DATE_SUB(NOW(), INTERVAL 1 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), 19.937000, 50.061400); -- Rynek Główny
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 5 HOUR), 19.937160, 50.061901), -- Floriańska
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 3 HOUR), 19.937050, 50.061500), -- W drodze blisko Rynku
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.937000, 50.061400); -- Rynek Główny

-- Paczka 15: Od Piotra (6 - Kraków) dla Jana (2 - Warszawa), kurier Marek (8), status "In Delivery" (w drodze do Warszawy)
-- Origin: ul. Karmelicka 20 (ID 6), Dest: ul. Piękna 5A, Warszawa (ID 2)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531015', 6, 2, 8, 'Medium', 2.8, 'Album ze zdjęciami.', 6, 2, DATE_SUB(NOW(), INTERVAL 8 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 20.200000, 51.000000); -- Na trasie KRK-WWA
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 9 HOUR), 19.932900, 50.064910), -- Karmelicka
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 8 HOUR), 20.200000, 51.000000); -- Na trasie

-- Dodaj więcej paczek i historii według potrzeb, zmieniając ID, daty, statusy i lokalizacje.
-- Poniżej kilka dodatkowych przykładów dla kurierów z Krakowa:

-- Paczka 16: Kurier Marek (8), status "Sent" z os. Złotej Jesieni (ID 8) do ul. Starowiślna (ID 12)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531016', 5, 6, 8, 'Small', 0.7, 'Kosmetyki', 8, 12, DATE_SUB(NOW(), INTERVAL 15 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 20.029970, 50.076050); -- os. Złotej Jesieni
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 15 MINUTE), 20.029970, 50.076050);

-- Paczka 17: Kurier Tomasz (9), status "In Delivery" z ul. Grzegórzecka (ID 13) do ul. Lea (ID 14)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531017', 7, 10, 9, 'Medium', 1.5, 'Akcesoria komputerowe', 13, 14, DATE_SUB(NOW(), INTERVAL 25 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 19.928880, 50.064770); -- Okolice Placu Inwalidów
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.959580, 50.058700), -- Grzegórzecka
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 25 MINUTE), 19.928880, 50.064770);

-- Paczka 18: Kurier Marek (8), status "Delivered" z ul. Bronowicka (ID 15) na Plac Inwalidów (ID 16)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531018', 10, 5, 8, 'Large', 6.0, 'Artykuły papiernicze', 15, 16, DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_SUB(NOW(), INTERVAL 30 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), 19.928880, 50.064770); -- Plac Inwalidów
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 4 HOUR), 19.898310, 50.074720), -- Bronowicka
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 2 HOUR), 19.910000, 50.070000), -- W drodze
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Delivered'), DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.928880, 50.064770); -- Plac Inwalidów

-- Paczka 19: Kurier Tomasz (9), status "Sent" z ul. Szewska (ID 17) do ul. Floriańska (ID 5)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531019', 6, 7, 9, 'Small', 0.4, 'List polecony', 17, 5, DATE_SUB(NOW(), INTERVAL 5 MINUTE), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), 19.935560, 50.061670); -- Szewska
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 5 MINUTE), 19.935560, 50.061670);

-- Paczka 20: Od Admina Krakow (11) dla Piotra (6), kurier Marek (8), status "In Delivery", dostawa tego samego dnia
-- Origin: ul. Lubicz 4 (ID 11), Dest: ul. Karmelicka 20 (ID 6)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531020', 11, 6, 8, 'Medium', 2.2, 'Ekspresowa dostawa', 11, 6, DATE_SUB(NOW(), INTERVAL 1 HOUR), (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), 19.939000, 50.063000); -- Okolice Rynku
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'), DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.948700, 50.063390), -- Lubicz
(@package_id, (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Delivery'), DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.939000, 50.063000); -- Okolice Rynku



-- tuuu


USE `trackandtrace`;

-- ###############################################################################
-- ## KROK 1: (OPCJONALNE) Usunięcie wcześniej dodanych "dodatkowych" paczek ##
-- ## aby uniknąć konfliktów, jeśli ten skrypt jest uruchamiany ponownie. ##
-- ###############################################################################

SET @trackingNumbersToDelete = "'TT20250531006', 'TT20250531007', 'TT20250531008', 'TT20250531009', 'TT20250531010', 'TT20250531011', 'TT20250531012', 'TT20250531013', 'TT20250531014', 'TT20250531015', 'TT20250531016', 'TT20250531017', 'TT20250531018', 'TT20250531019', 'TT20250531020'";

-- Usuwanie z PackageHistory
SET @sql_delete_history = CONCAT('DELETE FROM `PackageHistory` WHERE `PackageId` IN (SELECT `PackageId` FROM `Packages` WHERE `TrackingNumber` IN (', @trackingNumbersToDelete, '))');
PREPARE stmt_delete_history FROM @sql_delete_history;
EXECUTE stmt_delete_history;
DEALLOCATE PREPARE stmt_delete_history;

-- Usuwanie z Packages
SET @sql_delete_packages = CONCAT('DELETE FROM `Packages` WHERE `TrackingNumber` IN (', @trackingNumbersToDelete, ')');
PREPARE stmt_delete_packages FROM @sql_delete_packages;
EXECUTE stmt_delete_packages;
DEALLOCATE PREPARE stmt_delete_packages;

-- ###############################################################################
-- ## KROK 2: Dynamiczne pobieranie ID użytkowników i adresów ##
-- ## Te zapytania pobiorą ID na podstawie danych, które powinny już istnieć ##
-- ## w Twojej bazie po uruchomieniu początkowego schematu i ręcznej rejestracji. ##
-- ###############################################################################

-- Pobieranie UserID
SELECT UserId INTO @userId_AdminGlobal FROM Users WHERE Username = 'admin' LIMIT 1;
SELECT UserId INTO @userId_JanKowalski FROM Users WHERE Username = 'jan_kowalski' LIMIT 1;
SELECT UserId INTO @userId_AnnaLimanowska FROM Users WHERE Username = 'anna_limanowska' LIMIT 1;
SELECT UserId INTO @userId_KurierKarol FROM Users WHERE Username = 'kurier_waw' LIMIT 1; -- Zakładając, że 'kurier_waw' to Karol Kurierski
SELECT UserId INTO @userId_EwaNowak FROM Users WHERE Username = 'ewa_nowak' LIMIT 1;
SELECT UserId INTO @userId_PiotrZielinski FROM Users WHERE Username = 'piotr_zielinski' LIMIT 1;
SELECT UserId INTO @userId_MagdaWisniewska FROM Users WHERE Username = 'magda_wisniewska' LIMIT 1;
SELECT UserId INTO @userId_KurierMarek FROM Users WHERE Username = 'kurier_krak1' LIMIT 1; -- Marek Szybki
SELECT UserId INTO @userId_KurierTomasz FROM Users WHERE Username = 'kurier_krak2' LIMIT 1; -- Tomasz Niezawodny
SELECT UserId INTO @userId_KasiaKowal FROM Users WHERE Username = 'kasia_kowal' LIMIT 1;
SELECT UserId INTO @userId_AdminKrakow FROM Users WHERE Username = 'admin_krakow' LIMIT 1; -- Alicja Zarządna

-- Pobieranie AddressID (dla adresów z Twojego początkowego skryptu)
SELECT AddressId INTO @addressId_RynekGlowny1 FROM Addresses WHERE Street = 'Rynek Główny 1' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Piekna5A_WAW FROM Addresses WHERE Street = 'ul. Piękna 5A' AND City = 'Warszawa' LIMIT 1;
SELECT AddressId INTO @addressId_Jerozolimskie100_WAW FROM Addresses WHERE Street = 'Aleje Jerozolimskie 100' AND City = 'Warszawa' LIMIT 1;
SELECT AddressId INTO @addressId_Sezamkowa12_GD FROM Addresses WHERE Street = 'ul. Sezamkowa 12' AND City = 'Gdańsk' LIMIT 1;

-- Pobieranie AddressID (dla adresów dodanych w poprzednim skrypcie "dodatkowe dane")
SELECT AddressId INTO @addressId_Florianska15 FROM Addresses WHERE Street = 'ul. Floriańska 15' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Karmelicka20 FROM Addresses WHERE Street = 'ul. Karmelicka 20' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Dluga72 FROM Addresses WHERE Street = 'ul. Długa 72' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_ZlotejJesieni5 FROM Addresses WHERE Street = 'os. Złotej Jesieni 5/22' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Wielicka28 FROM Addresses WHERE Street = 'ul. Wielicka 28' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Zakopianska62 FROM Addresses WHERE Street = 'ul. Zakopiańska 62' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Lubicz4 FROM Addresses WHERE Street = 'ul. Lubicz 4' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Starowislna30 FROM Addresses WHERE Street = 'ul. Starowiślna 30' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Grzegorzecka60 FROM Addresses WHERE Street = 'ul. Grzegórzecka 60' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Lea112 FROM Addresses WHERE Street = 'ul. Lea 112' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Bronowicka80 FROM Addresses WHERE Street = 'ul. Bronowicka 80' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_PlacInwalidow6 FROM Addresses WHERE Street = 'Plac Inwalidów 6' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_Szewska2 FROM Addresses WHERE Street = 'ul. Szewska 2' AND City = 'Kraków' LIMIT 1;

-- Pobieranie StatusID
SELECT StatusId INTO @statusSent FROM StatusDefinitions WHERE Name = 'Sent' LIMIT 1;
SELECT StatusId INTO @statusInDelivery FROM StatusDefinitions WHERE Name = 'In Delivery' LIMIT 1;
SELECT StatusId INTO @statusDelivered FROM StatusDefinitions WHERE Name = 'Delivered' LIMIT 1;

-- ###############################################################################
-- ## KROK 3: Dodatkowe Paczki i Historia Paczek (Z UŻYCIEM ZMIENNYCH DLA ID) ##
-- ###############################################################################

-- Paczka 6: Od Ewy dla Piotra, kurier Marek, status "In Delivery" (w Krakowie)
-- Origin: Floriańska 15, Dest: Karmelicka 20
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531006', @userId_EwaNowak, @userId_PiotrZielinski, @userId_KurierMarek, 'Small', 0.5, 'Delikatne kwiaty.', @addressId_Florianska15, @addressId_Karmelicka20, DATE_SUB(NOW(), INTERVAL 3 HOUR), @statusInDelivery, 19.938344, 50.063419);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 4 HOUR), 19.937160, 50.061901),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 3 HOUR), 19.938344, 50.063419);

-- Paczka 7: Od Piotra dla Magdy, kurier Tomasz, status "Delivered" (w Krakowie)
-- Origin: Karmelicka 20, Dest: Długa 72
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531007', @userId_PiotrZielinski, @userId_MagdaWisniewska, @userId_KurierTomasz, 'Medium', 1.8, 'Książki uniwersyteckie.', @addressId_Karmelicka20, @addressId_Dluga72, DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY), @statusDelivered, 19.935390, 50.069910);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 2 DAY), 19.932900, 50.064910),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 1 DAY), 19.935000, 50.067000),
(@package_id, @statusDelivered, DATE_SUB(NOW(), INTERVAL 1 DAY), 19.935390, 50.069910);

-- Paczka 8: Od Magdy dla Ewy, kurier Marek, status "Sent" (w Krakowie)
-- Origin: Długa 72, Dest: Floriańska 15
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531008', @userId_MagdaWisniewska, @userId_EwaNowak, @userId_KurierMarek, 'Large', 5.2, 'Obraz.', @addressId_Dluga72, @addressId_Florianska15, DATE_SUB(NOW(), INTERVAL 30 MINUTE), @statusSent, 19.935390, 50.069910);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.935390, 50.069910);

-- Paczka 9: Od Kasi dla admina_krakow, kurier Tomasz, status "In Delivery" (w Krakowie)
-- Origin: Wielicka 28, Dest: Lubicz 4
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531009', @userId_KasiaKowal, @userId_AdminKrakow, @userId_KurierTomasz, 'Small', 0.3, 'Dokumenty firmowe.', @addressId_Wielicka28, @addressId_Lubicz4, DATE_SUB(NOW(), INTERVAL 1 HOUR), @statusInDelivery, 19.952700, 50.060830);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 2 HOUR), 20.000850, 50.032150),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.952700, 50.060830);

-- Paczka 10: Od admina globalnego dla Kasi, kurier Marek, status "Sent" (z Krakowa do Krakowa)
-- Origin: Rynek Główny 1, Dest: Zakopiańska 62
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TT20250531010', @userId_AdminGlobal, @userId_KasiaKowal, @userId_KurierMarek, 'Medium', 2.0, 'Prezent urodzinowy.', @addressId_RynekGlowny1, @addressId_Zakopianska62, DATE_SUB(NOW(), INTERVAL 10 MINUTE), @statusSent, 19.937000, 50.061400);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 10 MINUTE), 19.937000, 50.061400);

-- Paczka 11: Od Piotra dla Admina_Krakow, kurier Tomasz, status "In Delivery" (w Krakowie)
-- Origin: Karmelicka 20, Dest: os. Złotej Jesieni 5/22
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531011', @userId_PiotrZielinski, @userId_AdminKrakow, @userId_KurierTomasz, 'Small', 1.0, 'Gadżety firmowe.', @addressId_Karmelicka20, @addressId_ZlotejJesieni5, DATE_SUB(NOW(), INTERVAL 2 HOUR), @statusInDelivery, 20.015000, 50.070000);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 3 HOUR), 19.932900, 50.064910),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 2 HOUR), 20.015000, 50.070000);

-- Paczka 12: Od Anny (Warszawa) dla Magdy (Kraków), kurier Marek, status "In Delivery" (na trasie do Krakowa)
-- Origin: Aleje Jerozolimskie 100, Warszawa, Dest: Długa 72, Kraków
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531012', @userId_AnnaLimanowska, @userId_MagdaWisniewska, @userId_KurierMarek, 'Medium', 3.5, 'Artykuły biurowe.', @addressId_Jerozolimskie100_WAW, @addressId_Dluga72, DATE_SUB(NOW(), INTERVAL 6 HOUR), @statusInDelivery, 20.500000, 50.800000);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 7 HOUR), 21.012229, 52.229676),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 6 HOUR), 20.500000, 50.800000);

-- Paczka 13: Od Jana (Warszawa) dla Kasi (Kraków), kurier Karol (Warszawa), status "Sent"
-- Origin: ul. Piękna 5A, Warszawa, Dest: ul. Lea 112, Kraków
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531013', @userId_JanKowalski, @userId_KasiaKowal, @userId_KurierKarol, 'Large', 7.1, 'Monitor komputerowy.', @addressId_Piekna5A_WAW, @addressId_Lea112, DATE_SUB(NOW(), INTERVAL 45 MINUTE), @statusSent, 21.011000, 52.221000);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 45 MINUTE), 21.011000, 52.221000);

-- Paczka 14: Od Ewy (Kraków) dla Admina globalnego (Kraków), kurier Tomasz, status "Delivered"
-- Origin: Floriańska 15, Dest: Rynek Główny 1
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531014', @userId_EwaNowak, @userId_AdminGlobal, @userId_KurierTomasz, 'Small', 0.2, 'Pendrive z danymi.', @addressId_Florianska15, @addressId_RynekGlowny1, DATE_SUB(NOW(), INTERVAL 5 HOUR), DATE_SUB(NOW(), INTERVAL 1 HOUR), @statusDelivered, 19.937000, 50.061400);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 5 HOUR), 19.937160, 50.061901),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 3 HOUR), 19.937050, 50.061500),
(@package_id, @statusDelivered, DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.937000, 50.061400);

-- Paczka 15: Od Piotra (Kraków) dla Jana (Warszawa), kurier Marek, status "In Delivery"
-- Origin: Karmelicka 20, Dest: Piękna 5A, Warszawa
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531015', @userId_PiotrZielinski, @userId_JanKowalski, @userId_KurierMarek, 'Medium', 2.8, 'Album ze zdjęciami.', @addressId_Karmelicka20, @addressId_Piekna5A_WAW, DATE_SUB(NOW(), INTERVAL 8 HOUR), @statusInDelivery, 20.200000, 51.000000);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 9 HOUR), 19.932900, 50.064910),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 8 HOUR), 20.200000, 51.000000);

-- Paczka 16: Kurier Marek, status "Sent" z os. Złotej Jesieni do ul. Starowiślna
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531016', @userId_EwaNowak, @userId_PiotrZielinski, @userId_KurierMarek, 'Small', 0.7, 'Kosmetyki', @addressId_ZlotejJesieni5, @addressId_Starowislna30, DATE_SUB(NOW(), INTERVAL 15 MINUTE), @statusSent, 20.029970, 50.076050);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 15 MINUTE), 20.029970, 50.076050);

-- Paczka 17: Kurier Tomasz, status "In Delivery" z ul. Grzegórzecka do ul. Lea
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531017', @userId_MagdaWisniewska, @userId_KasiaKowal, @userId_KurierTomasz, 'Medium', 1.5, 'Akcesoria komputerowe', @addressId_Grzegorzecka60, @addressId_Lea112, DATE_SUB(NOW(), INTERVAL 25 MINUTE), @statusInDelivery, 19.928880, 50.064770);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.959580, 50.058700),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 25 MINUTE), 19.928880, 50.064770);

-- Paczka 18: Kurier Marek, status "Delivered" z ul. Bronowicka na Plac Inwalidów
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531018', @userId_KasiaKowal, @userId_EwaNowak, @userId_KurierMarek, 'Large', 6.0, 'Artykuły papiernicze', @addressId_Bronowicka80, @addressId_PlacInwalidow6, DATE_SUB(NOW(), INTERVAL 4 HOUR), DATE_SUB(NOW(), INTERVAL 30 MINUTE), @statusDelivered, 19.928880, 50.064770);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 4 HOUR), 19.898310, 50.074720),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 2 HOUR), 19.910000, 50.070000),
(@package_id, @statusDelivered, DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.928880, 50.064770);

-- Paczka 19: Kurier Tomasz, status "Sent" z ul. Szewska do ul. Floriańska
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531019', @userId_PiotrZielinski, @userId_MagdaWisniewska, @userId_KurierTomasz, 'Small', 0.4, 'List polecony', @addressId_Szewska2, @addressId_Florianska15, DATE_SUB(NOW(), INTERVAL 5 MINUTE), @statusSent, 19.935560, 50.061670);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 5 MINUTE), 19.935560, 50.061670);

-- Paczka 20: Od Admina Krakow dla Piotra, kurier Marek, status "In Delivery", dostawa tego samego dnia
-- Origin: ul. Lubicz 4, Dest: ul. Karmelicka 20
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TT20250531020', @userId_AdminKrakow, @userId_PiotrZielinski, @userId_KurierMarek, 'Medium', 2.2, 'Ekspresowa dostawa', @addressId_Lubicz4, @addressId_Karmelicka20, DATE_SUB(NOW(), INTERVAL 1 HOUR), @statusInDelivery, 19.939000, 50.063000);
SET @package_id = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id, @statusSent, DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.948700, 50.063390),
(@package_id, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.939000, 50.063000);





INSERT IGNORE INTO `Addresses` (`AddressId`, `Street`, `City`, `ZipCode`, `Country`) VALUES
(18, 'ul. Szlak 77', 'Kraków', '31-153', 'Polska'), -- Adres dla użytkownika "demo_user"
(19, 'ul. Mogilska 43', 'Kraków', '31-545', 'Polska'); -- Adres dla kuriera "demo_kurier"













-- Pobieranie UserID dla sztandarowych użytkowników
SELECT UserId INTO @userId_DemoUser FROM Users WHERE Username = 'demo_user' LIMIT 1;
SELECT UserId INTO @userId_DemoKurier FROM Users WHERE Username = 'demo_kurier' LIMIT 1;

-- Pobieranie AddressID dla adresów sztandarowych użytkowników
SELECT AddressId INTO @addressId_DemoUser FROM Addresses WHERE Street = 'ul. Szlak 77' AND City = 'Kraków' LIMIT 1;
SELECT AddressId INTO @addressId_DemoKurier FROM Addresses WHERE Street = 'ul. Mogilska 43' AND City = 'Kraków' LIMIT 1;

-- Pobieranie ID dla innych, istniejących użytkowników i adresów, którzy będą brali udział w paczkach pokazowych
-- Załóżmy, że 'ewa_nowak' i jej adres na 'ul. Floriańska 15' już istnieją i chcemy ich użyć.
SELECT UserId INTO @userId_EwaNowak FROM Users WHERE Username = 'ewa_nowak' LIMIT 1;
SELECT AddressId INTO @addressId_Florianska15 FROM Addresses WHERE Street = 'ul. Floriańska 15' AND City = 'Kraków' LIMIT 1;
-- Jeśli potrzebujesz więcej istniejących użytkowników/adresów, dodaj tutaj odpowiednie SELECT INTO.

-- Pobieranie StatusID
SELECT StatusId INTO @statusSent FROM StatusDefinitions WHERE Name = 'Sent' LIMIT 1;
SELECT StatusId INTO @statusInDelivery FROM StatusDefinitions WHERE Name = 'In Delivery' LIMIT 1;
SELECT StatusId INTO @statusDelivered FROM StatusDefinitions WHERE Name = 'Delivered' LIMIT 1;

-- ###############################################################################
-- ## PACZKI DLA SZTANDAROWYCH UŻYTKOWNIKÓW ##
-- ###############################################################################

-- Paczka P1: demo_user wysyła do ewa_nowak, demo_kurier dostarcza, status "In Delivery"
-- Origin: Adres demo_user (ul. Szlak 77), Dest: Adres ewa_nowak (ul. Floriańska 15)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TTDEMO001', @userId_DemoUser, @userId_EwaNowak, @userId_DemoKurier, 'Medium', 1.2, 'Dokumenty do prezentacji, pilne!', @addressId_DemoUser, @addressId_Florianska15, DATE_SUB(NOW(), INTERVAL 2 HOUR), @statusInDelivery, 19.939000, 50.063000); -- Przykładowa lokalizacja w Krakowie (np. okolice Rynku)
SET @package_id_p1 = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id_p1, @statusSent, DATE_SUB(NOW(), INTERVAL 2 HOUR), 19.940500, 50.067700), -- Współrzędne dla ul. Szlak
(@package_id_p1, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 1 HOUR), 19.939000, 50.063000); -- Współrzędne dla okolic Rynku

-- Paczka P2: ewa_nowak wysyła do demo_user, demo_kurier dostarcza, status "Delivered"
-- Origin: Adres ewa_nowak (ul. Floriańska 15), Dest: Adres demo_user (ul. Szlak 77)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TTDEMO002', @userId_EwaNowak, @userId_DemoUser, @userId_DemoKurier, 'Small', 0.8, 'Klucze zapasowe.', @addressId_Florianska15, @addressId_DemoUser, DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 20 HOUR), @statusDelivered, 19.940500, 50.067700); -- Współrzędne dla ul. Szlak (miejsce dostarczenia)
SET @package_id_p2 = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id_p2, @statusSent, DATE_SUB(NOW(), INTERVAL 1 DAY), 19.937160, 50.061901), -- Współrzędne dla ul. Floriańska
(@package_id_p2, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 22 HOUR), 19.938000, 50.065000), -- Gdzieś w drodze
(@package_id_p2, @statusDelivered, DATE_SUB(NOW(), INTERVAL 20 HOUR), 19.940500, 50.067700); -- Współrzędne dla ul. Szlak

-- Paczka P3: demo_user wysyła do demo_kurier (np. zwrot sprzętu), demo_kurier "sam sobie" przypisuje, status "Sent"
-- Origin: Adres demo_user (ul. Szlak 77), Dest: Adres demo_kurier (ul. Mogilska 43)
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TTDEMO003', @userId_DemoUser, @userId_DemoKurier, @userId_DemoKurier, 'Large', 3.0, 'Zwrot skanera.', @addressId_DemoUser, @addressId_DemoKurier, DATE_SUB(NOW(), INTERVAL 30 MINUTE), @statusSent, 19.940500, 50.067700); -- Współrzędne dla ul. Szlak
SET @package_id_p3 = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id_p3, @statusSent, DATE_SUB(NOW(), INTERVAL 30 MINUTE), 19.940500, 50.067700);

-- Paczka P4: demo_kurier wysyła (jako user prywatnie) do ewa_nowak, inny kurier (np. @userId_KurierMarek) dostarcza, status "In Delivery"
-- Origin: Adres demo_kurier (ul. Mogilska 43), Dest: Adres ewa_nowak (ul. Floriańska 15)
-- Potrzebujemy ID kuriera Marka Szybkiego - zakładam, że masz już takiego użytkownika.
-- Jeśli nie, zastąp @userId_KurierMarek ID innego istniejącego kuriera.
SELECT UserId INTO @userId_KurierMarek_Real FROM Users WHERE Username = 'kurier_krak1' LIMIT 1; -- Pobierz ID Marka

INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES 
('TTDEMO004', @userId_DemoKurier, @userId_EwaNowak, @userId_KurierMarek_Real, 'Small', 0.2, 'List prywatny.', @addressId_DemoKurier, @addressId_Florianska15, DATE_SUB(NOW(), INTERVAL 5 HOUR), @statusInDelivery, 19.948000, 50.062000); -- Gdzieś na Grzegórzkach
SET @package_id_p4 = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id_p4, @statusSent, DATE_SUB(NOW(), INTERVAL 5 HOUR), 19.968000, 50.062700), -- Współrzędne dla ul. Mogilska
(@package_id_p4, @statusInDelivery, DATE_SUB(NOW(), INTERVAL 4 HOUR), 19.948000, 50.062000); -- Gdzieś na Grzegórzkach

-- Paczka P5: demo_user wysyła do demo_user (np. do innego swojego punktu), demo_kurier dostarcza, status "Sent"
-- Origin: Adres demo_user (ul. Szlak 77), Dest: Inny adres w Krakowie, np. ul. Lea 112 (AddressId: 14 z poprzedniego skryptu)
SELECT AddressId INTO @addressId_Lea112 FROM Addresses WHERE Street = 'ul. Lea 112' AND City = 'Kraków' LIMIT 1;
INSERT INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `SubmissionDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
('TTDEMO005', @userId_DemoUser, @userId_DemoUser, @userId_DemoKurier, 'Medium', 1.5, 'Przesyłka wewnętrzna.', @addressId_DemoUser, @addressId_Lea112, DATE_SUB(NOW(), INTERVAL 10 MINUTE), @statusSent, 19.940500, 50.067700); -- Współrzędne dla ul. Szlak
SET @package_id_p5 = LAST_INSERT_ID();
INSERT INTO `PackageHistory` (`PackageId`, `StatusId`, `Timestamp`, `Longitude`, `Latitude`) VALUES
(@package_id_p5, @statusSent, DATE_SUB(NOW(), INTERVAL 10 MINUTE), 19.940500, 50.067700);





