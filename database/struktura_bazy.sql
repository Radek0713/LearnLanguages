-- Utworzenie bazy danych (jeśli jeszcze nie istnieje)
CREATE DATABASE IF NOT EXISTS nauka_jezykow 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE nauka_jezykow;

-- Czyszczenie starych tabel (w kolejności odwrotnej do zależności)
DROP TABLE IF EXISTS fiszki;
DROP TABLE IF EXISTS gramatyka;
DROP TABLE IF EXISTS temat;
DROP TABLE IF EXISTS jezyk;

-- 1. Tabela: Jezyk
CREATE TABLE jezyk (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nazwa VARCHAR(100) NOT NULL
) ENGINE=InnoDB;

-- 2. Tabela: Temat
CREATE TABLE temat (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_jezyka INT NOT NULL,
    nazwa VARCHAR(150) NOT NULL,
    CONSTRAINT fk_temat_jezyk 
        FOREIGN KEY (id_jezyka) 
        REFERENCES jezyk(id) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE
) ENGINE=InnoDB;

-- 3. Tabela: Gramatyka
CREATE TABLE gramatyka (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_jezyka INT NOT NULL,
    nazwa VARCHAR(150) NOT NULL,
    CONSTRAINT fk_gramatyka_jezyk 
        FOREIGN KEY (id_jezyka) 
        REFERENCES jezyk(id) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE
) ENGINE=InnoDB;

-- 4. Tabela: Fiszki
CREATE TABLE fiszki (
    id INT AUTO_INCREMENT PRIMARY KEY,
    id_tematu INT NOT NULL,
    nazwa_1 VARCHAR(255) NOT NULL,
    nazwa_2 VARCHAR(255) NOT NULL,
    grafika LONGBLOB NULL,
    data_ostatniej_powtorki DATE NULL,
    data_nastepnej_powtorki DATE NULL,
    wspolczynnik_latwosci INT DEFAULT 250,
    odstep_powtorek INT DEFAULT 0,
    liczba_poprawnych_powtorek INT DEFAULT 0,
    CONSTRAINT fk_fiszki_temat 
        FOREIGN KEY (id_tematu) 
        REFERENCES temat(id) 
        ON DELETE CASCADE 
        ON UPDATE CASCADE
) ENGINE=InnoDB;

-- Indeksy usprawniające zapytania ze strony C# (np. pobieranie fiszek dla danego tematu)
CREATE INDEX idx_temat_jezyk ON temat(id_jezyka);
CREATE INDEX idx_gramatyka_jezyk ON gramatyka(id_jezyka);
CREATE INDEX idx_fiszki_temat ON fiszki(id_tematu);
CREATE INDEX idx_fiszki_data_powtorki ON fiszki(data_nastepnej_powtorki);