-- Utworzenie bazy danych (jeśli jeszcze nie istnieje)
CREATE DATABASE IF NOT EXISTS nauka_jezykow 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE nauka_jezykow;

-- 1. Tabela: Jezyk
CREATE TABLE IF NOT EXISTS Jezyk (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nazwa VARCHAR(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Tabela: Dzial
CREATE TABLE IF NOT EXISTS Dzial (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nazwa VARCHAR(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Tabela: Temat
CREATE TABLE IF NOT EXISTS Temat (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_dzialu INT NOT NULL,
    nazwa VARCHAR(100) NOT NULL,
    -- czy_wybrany TINYINT NOT NULL DEFAULT 0,
    CONSTRAINT fk_temat_dzial 
        FOREIGN KEY (id_dzialu) REFERENCES Dzial(id) 
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. Tabela: Slowko
CREATE TABLE IF NOT EXISTS Slowko (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_tematu INT NOT NULL,
    nazwa VARCHAR(255) NOT NULL,
    grafika VARCHAR(255),
    CONSTRAINT fk_slowko_temat 
        FOREIGN KEY (id_tematu) REFERENCES Temat(id) 
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. Tabela: Tlumaczenie
CREATE TABLE IF NOT EXISTS Tlumaczenie (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_slowka INT NOT NULL,
    id_jezyka INT NOT NULL,
    nazwa VARCHAR(255) NOT NULL,
    grafika VARCHAR(255),
    -- data_ostatniej_powtorki DATE NULL,
    -- data_nastepnej_powtorki DATE NULL,
    -- wspolczynnik_latwosci DOUBLE DEFAULT 2.5,
    -- odstep_powtorek INT DEFAULT 0,
    -- liczba_poprawnych_powtorek INT DEFAULT 0,
    CONSTRAINT fk_tlumaczenie_slowko 
        FOREIGN KEY (id_slowka) REFERENCES Slowko(id) 
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_tlumaczenie_jezyk 
        FOREIGN KEY (id_jezyka) REFERENCES Jezyk(id) 
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. Tabela: Gramatyka
CREATE TABLE IF NOT EXISTS Gramatyka (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_jezyka INT NOT NULL,
    nazwa VARCHAR(255) NOT NULL,
    CONSTRAINT fk_gramatyka_jezyk 
        FOREIGN KEY (id_jezyka) REFERENCES Jezyk(id) 
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Aktualizacja z profilami

-- 7. Lista profili użytkowników
CREATE TABLE IF NOT EXISTS profil (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nazwa VARCHAR(100) NOT NULL UNIQUE,
    data_utworzenia DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 8. Tematy wybrane do nauki przez konkretny profil
CREATE TABLE IF NOT EXISTS profil_temat (
    id_profilu INT NOT NULL,
    id_tematu INT NOT NULL,
    czy_wybrany TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id_profilu, id_tematu),
    CONSTRAINT fk_pt_profil FOREIGN KEY (id_profilu) REFERENCES profil(id) ON DELETE CASCADE,
    CONSTRAINT fk_pt_temat FOREIGN KEY (id_tematu) REFERENCES temat(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 9. Indywidualny postęp powtórek (SM-2) dla każdego profilu
CREATE TABLE IF NOT EXISTS profil_postep (
    id_profilu INT NOT NULL,
    id_tlumaczenia INT NOT NULL,
    wspolczynnik_latwosci DOUBLE NOT NULL DEFAULT 2.5,
    odstep_powtorek INT NOT NULL DEFAULT 0,
    liczba_poprawnych_powtorek INT NOT NULL DEFAULT 0,
    data_nastepnej_powtorki DATE NOT NULL,
    PRIMARY KEY (id_profilu, id_tlumaczenia),
    CONSTRAINT fk_pp_profil FOREIGN KEY (id_profilu) REFERENCES profil(id) ON DELETE CASCADE,
    CONSTRAINT fk_pp_tlumaczenie FOREIGN KEY (id_tlumaczenia) REFERENCES tlumaczenie(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;