// ADDS Main Menu Dialog
// AutoCAD DCL - R14 compatible

adds_main_menu : dialog {
  label = "ADDS - Automated Drawing & Design System";
  width = 50;

  : row {
    : column {
      : button {
        key = "btn_draw_pipe";
        label = "Draw Pipe";
        width = 20;
      }
      : button {
        key = "btn_draw_vessel";
        label = "Draw Vessel";
        width = 20;
      }
      : button {
        key = "btn_place_pump";
        label = "Place Pump";
        width = 20;
      }
      : button {
        key = "btn_place_hx";
        label = "Place Heat Exchanger";
        width = 20;
      }
    }
    : column {
      : button {
        key = "btn_route_pipe";
        label = "Route Pipe";
        width = 20;
      }
      : button {
        key = "btn_equipment_rpt";
        label = "Equipment Report";
        width = 20;
      }
      : button {
        key = "btn_db_sync";
        label = "Sync Database";
        width = 20;
      }
      : button {
        key = "btn_settings";
        label = "Settings";
        width = 20;
      }
    }
  }

  ok_cancel;
}

adds_settings : dialog {
  label = "ADDS Settings";

  : edit_box {
    key = "oracle_host";
    label = "Oracle Host";
    edit_width = 30;
  }
  : edit_box {
    key = "oracle_port";
    label = "Oracle Port";
    edit_width = 10;
  }
  : edit_box {
    key = "oracle_sid";
    label = "Oracle SID";
    edit_width = 20;
  }
  : edit_box {
    key = "oracle_user";
    label = "Username";
    edit_width = 20;
  }
  : edit_box {
    key = "oracle_pass";
    label = "Password";
    edit_width = 20;
    password_char = "*";
  }
  ok_cancel;
}
