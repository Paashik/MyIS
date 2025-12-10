// Временные минимальные типы, чтобы TypeScript корректно обрабатывал React/AntD/Router/Vite
// даже без установленных node_modules. Эти декларации НЕ для продакшена, а для разработки
// каркаса, пока не установлены реальные @types/*.

// =======================
// React
// =======================
declare namespace React {
  type ReactNode = any;

  interface FC<P = {}> {
    (props: P & { children?: ReactNode }): any;
  }

  type PropsWithChildren<P = {}> = P & { children?: ReactNode };

  function createContext<T>(defaultValue: T): any;
  function useContext<T>(ctx: any): T;

  // Упрощённая сигнатура, чтобы не ругаться на setState(prev => ...)
  function useState<S = any>(
    initial: S | (() => S)
  ): [S, (value: S | ((prev: S) => S)) => void];

  function useEffect(effect: () => void | (() => void), deps?: any[]): void;

  function useMemo<T>(factory: () => T, deps: any[]): T;

  function useCallback<T extends (...args: any[]) => any>(
    cb: T,
    deps: any[]
  ): T;
}

declare module "react" {
  export = React;
  export as namespace React;

  export type FC<P = {}> = React.FC<P>;
  export type ReactNode = React.ReactNode;
  export type PropsWithChildren<P = {}> = React.PropsWithChildren<P>;

  export const StrictMode: any;

  export function createContext<T>(defaultValue: T): any;
  export function useContext<T>(ctx: any): T;

  export function useState<S = any>(
    initial: S | (() => S)
  ): [S, (value: S | ((prev: S) => S)) => void];

  export function useEffect(effect: () => void | (() => void), deps?: any[]): void;

  export function useMemo<T>(factory: () => T, deps: any[]): T;

  export function useCallback<T extends (...args: any[]) => any>(
    cb: T,
    deps: any[]
  ): T;
}

// jsx-runtime для режима "react-jsx"
declare module "react/jsx-runtime" {
  export const jsx: any;
  export const jsxs: any;
  export const Fragment: any;
}

// =======================
// ReactDOM
// =======================
declare module "react-dom/client" {
  export function createRoot(container: Element | DocumentFragment): {
    render(children: any): void;
  };
}

// =======================
// React Router DOM
// =======================
declare module "react-router-dom" {
  export const BrowserRouter: any;
  export const Routes: any;
  export const Route: any;
  export const Navigate: any;
  export const Outlet: any;
  export const Link: any;

  export function useNavigate(): (to: string, options?: any) => void;
  export function useLocation(): any;
}

// =======================
// Ant Design
// =======================
declare namespace AntD {
  interface FormComponent {
    (props: any): any;
    Item: any;
    // без дженерика, чтобы TS не ругался на вызовы с <T>
    useForm: (...args: any[]) => any;
  }
}

declare module "antd" {
  export const ConfigProvider: any;
  export const theme: any;
  export const Layout: any;
  export const Menu: any;
  export const Avatar: any;
  export const Typography: any;
  export const Dropdown: any;
  export const Space: any;
  export const Button: any;
  export const Alert: any;
  export const Spin: any;
  export const Card: any;
  export const Input: any;
  export const InputNumber: any;
  export const Checkbox: any;
  export const message: any;

  export type MenuProps = any;

  export const Form: AntD.FormComponent;
}

// =======================
// Ant Design Icons
// =======================
declare module "@ant-design/icons" {
  export const MenuFoldOutlined: any;
  export const MenuUnfoldOutlined: any;
  export const UserOutlined: any;
  export const ShoppingCartOutlined: any;
  export const TeamOutlined: any;
  export const DeploymentUnitOutlined: any;
  export const BuildOutlined: any;
  export const AppstoreOutlined: any;
  export const DatabaseOutlined: any;
  export const ApartmentOutlined: any;
}

// =======================
// Vite
// =======================
declare module "vite" {
  export function defineConfig(config: any): any;
}

declare module "@vitejs/plugin-react-swc" {
  const plugin: any;
  export default plugin;
}

// =======================
// JSX
// =======================

// Поддержка JSX, чтобы не было ошибок вида
// "JSX element implicitly has type 'any' because no interface 'JSX.IntrinsicElements' exists."
declare namespace JSX {
  interface IntrinsicElements {
    [elemName: string]: any;
  }
}