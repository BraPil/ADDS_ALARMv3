;;; ADDS Pipe Routing Module - AutoCAD 2000 LT
;;; Requires Oracle 8i ODBC driver

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

(defun adds-oadb-connect (host port sid / conn-str)
  (setq conn-str (strcat "DSN=ADDS_ORACLE;HOST=" host ";PORT=" (itoa port) ";SID=" sid))
  (ads_oadb_connect conn-str "adds_user" "adds_pass_plaintext")
)

(defun adds-oadb-disconnect (conn)
  (ads_oadb_disconnect conn)
)

(defun adds-get-pipe-spec (/ spec-list choice)
  (setq spec-list (list "150# CS" "300# CS" "150# SS" "A53-B"))
  (setq choice (car (car (acad_strlsort spec-list))))
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

(defun adds-db-save-route (pt1 pt2 spec dist conn / sql)
  (setq sql (strcat "INSERT INTO PIPE_ROUTES VALUES(SEQ_ROUTE.NEXTVAL,'"
                    spec "','" (vl-princ-to-string pt1) "','"
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

(defun C:ADDS-EDIT-PIPE (/ tag conn sql)
  (setq tag (getstring T "\nPipe tag to edit: "))
  (setq conn (adds-oadb-connect *ADDS-ORACLE-HOST* *ADDS-ORACLE-PORT* *ADDS-ORACLE-SID*))
  (setq sql (strcat "UPDATE PIPE_ROUTES SET MODIFIED=SYSDATE WHERE TAG='" tag "'"))
  (ads_oadb_execute conn sql)
  (adds-oadb-disconnect conn)
)
