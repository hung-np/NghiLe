#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::{fs, io::ErrorKind, path::PathBuf};
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Manager, PhysicalPosition, WebviewWindow, WindowEvent,
};
use tauri_plugin_window_state::{AppHandleExt, StateFlags, WindowExt};

const LAYOUT: StateFlags = StateFlags::POSITION.union(StateFlags::SIZE);

fn save_layout(app: &AppHandle) {
    let _ = app.save_window_state(LAYOUT);
}

#[tauri::command]
fn hide_widget(window: WebviewWindow) -> Result<(), String> {
    save_layout(window.app_handle());
    window.hide().map_err(|error| error.to_string())
}

#[tauri::command]
fn finish_resize(window: WebviewWindow) {
    fit_to_monitor(&window);
    save_layout(window.app_handle());
}

#[tauri::command]
fn list_music(app: AppHandle) -> Result<Vec<(String, String)>, String> {
    let folder = if cfg!(debug_assertions) {
        PathBuf::from(env!("CARGO_MANIFEST_DIR")).with_file_name("nhac")
    } else {
        std::env::current_exe()
            .map_err(|error| error.to_string())?
            .with_file_name("nhac")
    };
    let entries = match fs::read_dir(folder) {
        Ok(entries) => entries,
        Err(error) if error.kind() == ErrorKind::NotFound => return Ok(Vec::new()),
        Err(error) => return Err(error.to_string()),
    };
    let scope = app.asset_protocol_scope();
    let mut tracks = Vec::new();
    for entry in entries.filter_map(Result::ok) {
        if !entry.file_type().map(|kind| kind.is_file()).unwrap_or(false) {
            continue;
        }
        let path = entry.path();
        let extension = path.extension().and_then(|ext| ext.to_str()).unwrap_or("");
        if !matches!(extension.to_ascii_lowercase().as_str(), "mp3" | "wav" | "m4a") {
            continue;
        }
        let (Some(file), Some(name)) = (path.to_str(), path.file_name().and_then(|name| name.to_str())) else {
            continue;
        };
        if !scope.is_allowed(&path) {
            scope.allow_file(&path).map_err(|error| error.to_string())?;
        }
        tracks.push((file.to_owned(), name.to_owned()));
    }
    Ok(tracks)
}

fn fit_to_monitor(window: &WebviewWindow) {
    let (Ok(position), Ok(size), Ok(monitors)) =
        (window.outer_position(), window.outer_size(), window.available_monitors())
    else {
        return;
    };
    let fits = monitors.iter().any(|monitor| {
        let area = monitor.work_area();
        let left = i64::from(area.position.x);
        let top = i64::from(area.position.y);
        let x = i64::from(position.x);
        let y = i64::from(position.y);
        x >= left && y >= top
            && x + i64::from(size.width) <= left + i64::from(area.size.width)
            && y + i64::from(size.height) <= top + i64::from(area.size.height)
    });
    if fits {
        return;
    }
    if let Ok(Some(primary)) = window.primary_monitor() {
        let area = primary.work_area();
        let inset = (22.0 * primary.scale_factor()).round() as i32;
        let x = area.position.x + (area.size.width as i32 - size.width as i32 - inset).max(0);
        let y = area.position.y + (area.size.height as i32 - size.height as i32 - inset).max(0);
        let _ = window.set_position(PhysicalPosition::new(x, y));
    }
}

fn show_widget(app: &AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        fit_to_monitor(&window);
        let _ = window.show();
        let _ = window.set_focus();
    }
}

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, _, _| show_widget(app)))
        .plugin(tauri_plugin_window_state::Builder::default().with_state_flags(LAYOUT).build())
        .invoke_handler(tauri::generate_handler![hide_widget, finish_resize, list_music])
        .setup(|app| {
            let show = MenuItem::with_id(app, "show", "Mở widget", true, None::<&str>)?;
            let hide = MenuItem::with_id(app, "hide", "Ẩn widget", true, None::<&str>)?;
            let quit = MenuItem::with_id(app, "quit", "Thoát Nghỉ Lễ", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show, &hide, &quit])?;
            let tray = TrayIconBuilder::new().menu(&menu).show_menu_on_left_click(false)
                .tooltip("Nghỉ Lễ · Đếm ngược Tết");
            let tray = match app.default_window_icon() {
                Some(icon) => tray.icon(icon.clone()),
                None => tray,
            };
            tray.on_menu_event(|app, event| match event.id().as_ref() {
                "show" => show_widget(app),
                "hide" => {
                    if let Some(window) = app.get_webview_window("main") {
                        let _ = hide_widget(window);
                    }
                }
                "quit" => {
                    save_layout(app);
                    app.exit(0);
                }
                _ => {}
            })
            .on_tray_icon_event(|tray, event| {
                if let TrayIconEvent::Click {
                    button: MouseButton::Left, button_state: MouseButtonState::Up, ..
                } = event {
                    show_widget(tray.app_handle());
                }
            })
            .build(app)?;

            if let Some(window) = app.get_webview_window("main") {
                let _ = window.restore_state(LAYOUT);
            }
            show_widget(app.handle());
            Ok(())
        })
        .on_window_event(|window, event| match event {
            WindowEvent::CloseRequested { api, .. } => {
                api.prevent_close();
                save_layout(window.app_handle());
                let _ = window.hide();
            }
            WindowEvent::Focused(false) => save_layout(window.app_handle()),
            WindowEvent::ScaleFactorChanged { .. } => {
                if let Some(widget) = window.app_handle().get_webview_window("main") {
                    fit_to_monitor(&widget);
                }
            }
            _ => {}
        })
        .run(tauri::generate_context!())
        .expect("Không thể khởi động Nghỉ Lễ");
}
