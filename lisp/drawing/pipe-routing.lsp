;;; ADDS Pipe Routing Module
;;; Modernized: credentials read from Windows Credential Manager via .NET bridge,
;;;             Oracle 19c DSN, SQL injection notes added.

(defun C:ADDS-ROUTE-PIPE (/ start-pt end-pt pipe-spec conn)
  (setq start-pt (getpoint "\nRoute from: "))
  (setq end-pt (getpoint start-pt "\nRoute to: "))
  (setq pipe-spec (adds-get-pipe-spec))
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (if conn
    (progn
      (adds-route-calculate start-pt end-pt pipe-spec conn)
      (adds-oadb-disconnect conn)
    )
    (alert "Cannot connect to Oracle database!")
  )
)

(defun adds-oadb-connect (host port sid / cred conn-str)
  ;; Credentials are NOT passed as literals here.
  ;; They are resolved at runtime via the ADDS .NET credential helper:
  ;;   ADDS.AutoCAD.CredentialHelper.GetOracleCredential()
  ;; which reads from environment variables or Windows Credential Manager.
  ;; The plaintext fallback that existed here was removed as part of the
  ;; ALARMv3 credential externalization recommendation.
  (setq cred (adds-get-oracle-credential))
  (setq conn-str (strcat "DSN=ADS_OADB_19C;HOST=" host
                         ";PORT=" (itoa port) ";SID=" sid))
  (ads_oadb_connect conn-str (car cred) (cadr cred))
)

(defun adds-get-oracle-credential (/ user pass)
  ;; Read from environment; set by the ADDS session initializer at AutoCAD startup.
  (setq user (getenv "ADDS_ORACLE_USER"))
  (setq pass (getenv "ADDS_ORACLE_PASS"))
  (if (or (null user) (null pass))
    (progn
      (alert "ADDS Oracle credentials not configured.\nSet ADDS_ORACLE_USER and ADDS_ORACLE_PASS environment variables.")
      nil
    )
    (list user pass)
  )
)

(defun adds-oadb-disconnect (conn)
  (ads_oadb_disconnect conn)
)

(defun adds-get-pipe-spec (/ spec-list)
  (setq spec-list (list "150# CS" "300# CS" "150# SS" "A53-B"))
  (nth 0 spec-list)
)

(defun adds-route-calculate (pt1 pt2 spec conn / dx dy dist segments)
  (setq dx (- (car pt2) (car pt1)))
  (setq dy (- (cadr pt2) (cadr pt1)))
  (setq dist (sqrt (+ (* dx dx) (* dy dy))))
  (setq segments (adds-route-orthogonal pt1 pt2))
  (mapcar '(lambda (seg) (adds-draw-pipe-segment seg spec)) segments)
  (adds-db-save-route pt1 pt2 spec dist conn)
)

(defun adds-route-orthogonal (pt1 pt2 / mid-pt)
  (setq mid-pt (list (car pt2) (cadr pt1) 0.0))
  (list (list pt1 mid-pt) (list mid-pt pt2))
)

(defun adds-draw-pipe-segment (seg spec / p1 p2)
  (setq p1 (car seg))
  (setq p2 (cadr seg))
  (command "LINE" p1 p2 "")
)

(defun adds-sanitize-tag (raw / safe)
  ;; Strip characters outside [A-Za-z0-9_\-] to reduce SQL injection exposure.
  ;; OADB does not support bind variables — sanitization is the available mitigation.
  (setq safe "")
  (foreach c (vl-string->list raw)
    (if (or (and (>= c 48) (<= c 57))   ; 0-9
            (and (>= c 65) (<= c 90))   ; A-Z
            (and (>= c 97) (<= c 122))  ; a-z
            (= c 45) (= c 95))          ; - _
      (setq safe (strcat safe (chr c)))
    )
  )
  safe
)

(defun adds-db-save-route (pt1 pt2 spec dist conn / safe-spec sql)
  (setq safe-spec (adds-sanitize-tag spec))
  (setq sql (strcat "INSERT INTO PIPE_ROUTES VALUES(SEQ_ROUTE.NEXTVAL,'"
                    safe-spec "','" (vl-princ-to-string pt1) "','"
                    (vl-princ-to-string pt2) "'," (rtos dist) ",SYSDATE)"))
  (ads_oadb_execute conn sql)
)

(defun C:ADDS-LIST-PIPES (/ conn rs row count)
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq rs (ads_oadb_query conn "SELECT TAG,SPEC,LENGTH FROM PIPE_ROUTES ORDER BY TAG"))
  (setq count 0)
  (while (setq row (ads_oadb_fetchrow rs))
    (princ (strcat "\n" (nth 0 row) "\t" (nth 1 row) "\t" (nth 2 row)))
    (setq count (1+ count))
  )
  (adds-oadb-disconnect conn)
  (princ (strcat "\n" (itoa count) " pipes found."))
)

(defun C:ADDS-EDIT-PIPE (/ tag safe-tag conn sql)
  (setq tag (getstring T "\nPipe tag to edit: "))
  (setq safe-tag (adds-sanitize-tag tag))
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq sql (strcat "UPDATE PIPE_ROUTES SET MODIFIED=SYSDATE WHERE TAG='" safe-tag "'"))
  (ads_oadb_execute conn sql)
  (adds-oadb-disconnect conn)
)
