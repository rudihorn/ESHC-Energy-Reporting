-- Database: energy

-- DROP DATABASE energy;

CREATE DATABASE energy
  WITH OWNER = energy
       ENCODING = 'UTF8'
       TABLESPACE = pg_default
       LC_COLLATE = 'en_US.UTF-8'
       LC_CTYPE = 'en_US.UTF-8'
       CONNECTION LIMIT = -1;


-- Table: public.master_auths

-- DROP TABLE public.master_auths;

CREATE TABLE public.master_auths
(
  "user" text NOT NULL,
  name text NOT NULL,
  key text NOT NULL,
  CONSTRAINT master_auth_pkey PRIMARY KEY ("user")
)
WITH (
  OIDS=FALSE
);
ALTER TABLE public.master_auths
  OWNER TO energy;


-- Table: public.meter_types

-- DROP TABLE public.meter_types;

CREATE TABLE public.meter_types
(
type_id serial,
name text NOT NULL,
daily_quota real NOT NULL,
CONSTRAINT meter_types_pkey PRIMARY KEY (type_id)
)
WITH (
OIDS=FALSE
);
ALTER TABLE public.meter_types
OWNER TO energy;


-- Table: public.meters

-- DROP TABLE public.meters;

CREATE TABLE public.meters
(
  flat text,
  mpan text,
  serial text,
  disabled boolean NOT NULL DEFAULT false,
  meter_id bigserial,
  reset_value integer NOT NULL DEFAULT 100000,
  meter_type integer NOT NULL DEFAULT 1,
  CONSTRAINT meters_pkey PRIMARY KEY (meter_id),
  CONSTRAINT meters_meter_type_fkey FOREIGN KEY (meter_type)
    REFERENCES public.meter_types (type_id) MATCH SIMPLE
    ON UPDATE NO ACTION ON DELETE NO ACTION
)
WITH (
  OIDS=FALSE
);
ALTER TABLE public.meters
  OWNER TO energy;


-- Table: public.reminders

-- DROP TABLE public.reminders;

CREATE TABLE public.reminders
(
  flat text NOT NULL,
  last_sent date NOT NULL,
  since date NOT NULL,
  CONSTRAINT reminders_pkey PRIMARY KEY (flat)
)
WITH (
  OIDS=FALSE
);
ALTER TABLE public.reminders
  OWNER TO energy;


-- Table: public.user_auths

-- DROP TABLE public.user_auths;

CREATE TABLE public.user_auths
(
  "user" text NOT NULL,
  key text NOT NULL,
  flat text NOT NULL,
  name text NOT NULL,
  CONSTRAINT flat_authorizations_pkey PRIMARY KEY ("user")
)
WITH (
  OIDS=FALSE
);
ALTER TABLE public.user_auths
  OWNER TO energy;


-- Table: public.meter_readings

-- DROP TABLE public.meter_readings;

CREATE TABLE public.meter_readings
(
  date date,
  value integer,
  meter_id bigint,
  reading_id bigserial,
  CONSTRAINT meter_readings_pkey PRIMARY KEY (reading_id),
  CONSTRAINT meter_readings_meter_id_fkey FOREIGN KEY (meter_id)
      REFERENCES public.meters (meter_id) MATCH SIMPLE
      ON UPDATE NO ACTION ON DELETE RESTRICT
)
WITH (
  OIDS=FALSE
);
ALTER TABLE public.meter_readings
  OWNER TO energy;