;;; ADDS Drawing Commands
;;; Modernized: Oracle 19c globals, credentials read from config (not hardcoded)

(defun C:ADDS-DRAW-PIPE (/ pt1 pt2 dia layer-name)
  (setq layer-name "PIPE-STD")
  (setq pt1 (getpoint "\nStart point: "))
  (setq pt2 (getpoint pt1 "\nEnd point: "))
  (setq dia (getreal "\nDiameter <2.5>: "))
  (if (null dia) (setq dia 2.5))
  (command "LAYER" "M" layer-name "")
  (command "LINE" pt1 pt2 "")
  (adds-log-event "DRAW-PIPE" (list pt1 pt2 dia))
)

(defun C:ADDS-DRAW-VESSEL (/ ctr rad tag)
  (setq ctr (getpoint "\nCenter point: "))
  (setq rad (getreal "\nRadius <5.0>: "))
  (if (null rad) (setq rad 5.0))
  (setq tag (getstring T "\nVessel tag: "))
  (command "LAYER" "M" "VESSEL" "")
  (command "CIRCLE" ctr rad)
  (adds-draw-tag ctr tag)
  (adds-db-save-vessel tag ctr rad)
)

(defun adds-draw-tag (pt label / txt-ht)
  (setq txt-ht 0.125)
  (command "TEXT" "J" "MC" pt txt-ht 0 label)
)

(defun adds-log-event (event-type data / log-str)
  (setq log-str (strcat event-type ": " (vl-princ-to-string data)))
  (adds-write-log log-str)
)

(defun adds-write-log (msg / fh)
  (setq fh (open "C:\\ADDS\\adds.log" "a"))
  (write-line msg fh)
  (close fh)
)

(defun C:ADDS-DRAW-INSTRUMENT (/ pt tag type scale)
  (setq pt (getpoint "\nInstrument location: "))
  (setq tag (getstring T "\nTag number: "))
  (setq type (getstring "\nInstrument type <FT>: "))
  (if (= type "") (setq type "FT"))
  (setq scale *ADDS-DRAWING-SCALE*)
  (adds-insert-instrument-block type pt tag scale)
  (adds-db-save-instrument tag type pt)
)

(defun adds-insert-instrument-block (blk-type ins-pt tag-num scale / blk-name)
  (setq blk-name (strcat "INSTR-" blk-type))
  (command "INSERT" blk-name ins-pt scale scale 0)
  (command "ATTDEF" "" "TAG" "Enter tag:" tag-num ins-pt 0.09 0)
)

;;; ── Config loader ─────────────────────────────────────────────────────────
;;; Reads ADDS_ORACLE_HOST / PORT / SID from C:\ADDS\adds.config on first load.
;;; Credentials are never stored in the drawing or .lsp files.

(defun adds-load-config (/ fh line key val)
  (setq fh (open "C:\\ADDS\\adds.config" "r"))
  (if fh
    (progn
      (while (setq line (read-line fh))
        (if (wcmatch line "*=*")
          (progn
            (setq key (substr line 1 (- (vl-string-search "=" line) 0)))
            (setq val (substr line (+ (vl-string-search "=" line) 2)))
            (cond
              ((= key "ORACLE_HOST") (setq *ADDS-ORACLE-HOST* val))
              ((= key "ORACLE_PORT") (setq *ADDS-ORACLE-PORT* (atoi val)))
              ((= key "ORACLE_SID")  (setq *ADDS-ORACLE-SID*  val))
            )
          )
        )
      )
      (close fh)
    )
  )
)

;;; ── Global state ──────────────────────────────────────────────────────────
(setq *ADDS-DRAWING-SCALE* 1.0)
(setq *ADDS-CURRENT-PROJECT* nil)
(setq *ADDS-DB-CONNECTION* nil)
(setq *ADDS-USER-NAME* "")
(setq *ADDS-UNIT-SYSTEM* "IMPERIAL")
(setq *ADDS-LAYER-PREFIX* "ADDS-")
;;; Oracle 19c defaults — overridden at load time by adds-load-config
(setq *ADDS-ORACLE-HOST* "ORACLE19C-PROD")
(setq *ADDS-ORACLE-PORT* 1521)
(setq *ADDS-ORACLE-SID*  "ADDSDB")

(adds-load-config)
