import { describe, it, expect, vi, beforeEach } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { Component2020SettingsPage } from "./Component2020SettingsPage";
import * as api from "../api/adminComponent2020Api";
import { AuthProvider } from "../../../../../auth/AuthContext";

// Setup happy-dom environment
// Mock API
vi.mock("../api/adminComponent2020Api");

// Mock permissions to bypass access restrictions
vi.mock("../../../../../core/auth/permissions", () => ({
  useCan: vi.fn((permission: string) => {
    // Grant all permissions for tests
    return true;
  }),
}));

// Mock window methods for Ant Design in jsdom
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

Object.defineProperty(window, "requestAnimationFrame", {
  writable: true,
  value: vi.fn().mockImplementation((cb) => setTimeout(cb, 16)),
});

Object.defineProperty(window, "cancelAnimationFrame", {
  writable: true,
  value: vi.fn(),
});

Object.defineProperty(window, "getComputedStyle", {
  writable: true,
  value: vi.fn().mockImplementation(() => ({
    getPropertyValue: vi.fn(),
  })),
});

const Wrapper: React.FC<React.PropsWithChildren> = ({ children }) => (
  <BrowserRouter>
    <AuthProvider>{children}</AuthProvider>
  </BrowserRouter>
);

describe("Component2020SettingsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    
    vi.mocked(api.getComponent2020Connection).mockResolvedValue({
      id: "conn-1",
      mdbPath: "C:\\test.mdb",
      login: "testuser",
      hasPassword: true,
      isActive: true,
    });
    vi.mocked(api.getComponent2020Status).mockResolvedValue({
      isConnected: true,
      isSchedulerActive: false,
      lastSuccessfulSync: "2025-12-16T10:00:00Z",
      lastSyncStatus: "Success",
    });
    vi.mocked(api.getComponent2020SyncRuns).mockResolvedValue({
      runs: [],
      totalCount: 0,
    });
  });

  it("должна отображать заголовок страницы", async () => {
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText(/Интеграция — Component-2020/i)).toBeInTheDocument();
    });
  });

  it("должна загружать и отображать подключение", async () => {
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(api.getComponent2020Connection).toHaveBeenCalled();
    });
    const mdbPathInput = screen.getByTestId("component2020-mdb-path-input") as HTMLInputElement;
    expect(mdbPathInput).toBeInTheDocument();
  });

  it("должна устанавливать путь к файлу при выборе через file picker", async () => {
    const user = userEvent.setup();
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("component2020-mdb-path-browse"));
    
    const browseButton = screen.getByTestId("component2020-mdb-path-browse");
    
    // Mock console.log to reduce noise
    const consoleSpy = vi.spyOn(console, "log").mockImplementation(() => {});
    
    // Server-side MDB picker (modal + table)
    const fullPath = "C:\\\\Databases\\\\test_database.mdb";
    vi.mocked(api.getComponent2020FsEntries).mockResolvedValue({
      databasesRoot: "C:\\\\Databases",
      currentRelativePath: "",
      entries: [
        {
          name: "test_database.mdb",
          relativePath: "test_database.mdb",
          fullPath,
          isDirectory: false,
          sizeBytes: 123,
          lastWriteTimeUtc: "2025-12-16T10:00:00Z",
        },
      ],
    });

    await user.click(browseButton);

    await waitFor(() => {
      expect(api.getComponent2020FsEntries).toHaveBeenCalled();
    });

    const filesTable = await screen.findByTestId("component2020-mdb-files-table");
    const selectButton = within(filesTable).getByRole("button", { name: /^(Выбрать|Select)$/i });
    await user.click(selectButton);
    
    // Wait for form field to be updated
    await waitFor(() => {
      const mdbPathInput = screen.getByTestId("component2020-mdb-path-input") as HTMLInputElement;
      expect(mdbPathInput.value).toBe(fullPath);
    });
    
    consoleSpy.mockRestore();
  });

  it("должна показывать факт наличия пароля без значения", async () => {
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText(/Пароль задан/i)).toBeInTheDocument();
    });
    const passwordInput = screen.getByTestId("component2020-password") as HTMLInputElement;
    expect(passwordInput.value).toBe("");
  });

  it("должна вызывать testConnection при клике на кнопку", async () => {
    vi.mocked(api.testComponent2020Connection).mockResolvedValue({ isConnected: true });
    const user = userEvent.setup();
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("component2020-test-connection"));
    const testButton = screen.getByTestId("component2020-test-connection");
    await user.click(testButton);
    await waitFor(() => {
      expect(api.testComponent2020Connection).toHaveBeenCalled();
    });
  });

  it("должна вызывать saveConnection при клике на кнопку", async () => {
    vi.mocked(api.saveComponent2020Connection).mockResolvedValue();
    const user = userEvent.setup();
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("component2020-save-connection"));
    const saveButton = screen.getByTestId("component2020-save-connection");
    await user.click(saveButton);
    await waitFor(() => {
      expect(api.saveComponent2020Connection).toHaveBeenCalled();
    });
  });

  it("должна вызывать runSync при клике на кнопку", async () => {
    vi.mocked(api.runComponent2020Sync).mockResolvedValue({
      runId: "run-1",
      status: "Started",
      processedCount: 0,
    });
    const user = userEvent.setup();
    render(<Component2020SettingsPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByTestId("component2020-sync-scope"));
    const scopeSelect = screen.getByTestId("component2020-sync-scope");
    const selector = scopeSelect.querySelector(".ant-select-selector") as HTMLElement | null;
    expect(selector).not.toBeNull();
    fireEvent.mouseDown(selector as HTMLElement);
    await waitFor(() => {
      expect(document.querySelector(".ant-select-dropdown")).toBeTruthy();
    });
    const dropdown = document.querySelector(".ant-select-dropdown") as HTMLElement;
    const scopeOption = within(dropdown).getByTitle(/(Единицы измерения|Units)/i);
    await user.click(scopeOption);
    const runButton = screen.getByTestId("component2020-run-sync");
    await user.click(runButton);
    await waitFor(() => {
      expect(api.runComponent2020Sync).toHaveBeenCalled();
    });
  });
});
