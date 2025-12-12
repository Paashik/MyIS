import React, { useEffect, useState } from "react";
import { Alert, Button, Card, Spin, Typography } from "antd";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  CreateRequestPayload,
  RequestDto,
  RequestLineInputDto,
  RequestTypeDto,
  UpdateRequestPayload,
} from "../api/types";
import {
  createRequest,
  getRequest,
  getRequestTypes,
  updateRequest,
} from "../api/requestsApi";
import { RequestForm, RequestFormValues } from "../components/RequestForm";
import { t } from "../../../core/i18n/t";

const { Title } = Typography;

type PageState =
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "error"; message: string };

export const RequestEditPage: React.FC = () => {
  const { id } = useParams<{ id?: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const location = useLocation();

  type RequestsDirectionSegment = "incoming" | "outgoing";

  const directionFromPath: RequestsDirectionSegment = (() => {
    const seg = (location.pathname.split("/")[2] || "").toLowerCase();
    return seg === "outgoing" ? "outgoing" : "incoming";
  })();

  const returnContext = (() => {
    const sp = new URLSearchParams(location.search);

    const rawDirection = (sp.get("direction") || "").trim().toLowerCase();
    const direction: RequestsDirectionSegment = rawDirection === "outgoing" ? "outgoing" : "incoming";

    const rawType = sp.get("type");
    const type = ((rawType || "").trim() || "all");

    return { direction, type };
  })();

  // Для create: направление берём из сегмента URL (/requests/{direction}/new)
  // Для edit: направление нужно только для возврата в список/детали, поэтому берём из query (?direction=...)
  const direction: RequestsDirectionSegment = isEdit ? returnContext.direction : directionFromPath;

  const typeKeyFromQuery = (() => {
    const sp = new URLSearchParams(location.search);
    return (sp.get("type") || "").trim();
  })();

  const [state, setState] = useState<PageState>({ kind: "loading" });
  const [requestTypes, setRequestTypes] = useState<RequestTypeDto[]>([]);
  const [request, setRequest] = useState<RequestDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [createContextError, setCreateContextError] = useState<string | null>(null);
  const [fixedCreateTypeId, setFixedCreateTypeId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });

      try {
        const typesPromise = getRequestTypes();
        const requestPromise = isEdit && id ? getRequest(id) : Promise.resolve(null);

        const [types, existing] = await Promise.all([typesPromise, requestPromise]);

        if (cancelled) return;

        setRequestTypes(types);
        if (existing) {
          setRequest(existing as RequestDto);
        }

        // new: тип берём из query (?type=...) и не даём создавать без выбранного типа
        if (!isEdit) {
          const expectedDirection = direction === "incoming" ? "Incoming" : "Outgoing";

          if (!typeKeyFromQuery || typeKeyFromQuery === "all") {
            setCreateContextError(t("requests.edit.createContext.selectType"));
            setFixedCreateTypeId(null);
          } else {
            const found = types.find((x) => x.code === typeKeyFromQuery);
            if (!found || found.direction !== expectedDirection) {
              setCreateContextError(t("requests.edit.createContext.selectType"));
              setFixedCreateTypeId(null);
            } else {
              setCreateContextError(null);
              setFixedCreateTypeId(found.id);
            }
          }
        }

        setState({ kind: "loaded" });
      } catch (error) {
        if (cancelled) return;

        const message =
          error instanceof Error
            ? error.message
            : t("requests.edit.error.loadFormData");
        setState({ kind: "error", message });
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [direction, id, isEdit, typeKeyFromQuery]);

  const handleCancel = () => {
    if (isEdit && id) {
      navigate(
        `/requests/${encodeURIComponent(id)}?direction=${encodeURIComponent(returnContext.direction)}&type=${encodeURIComponent(returnContext.type)}`
      );
    } else {
      const typeParam = typeKeyFromQuery && typeKeyFromQuery !== "all" ? typeKeyFromQuery : "all";
      navigate(`/requests/${encodeURIComponent(direction)}?type=${encodeURIComponent(typeParam)}`);
    }
  };

  const handleSubmit = async (values: RequestFormValues) => {
    setSubmitting(true);
    setSaveError(null);

    try {
      if (isEdit && id) {
        const payload: UpdateRequestPayload = {
          title: values.title,
          description: values.description,
          lines: values.lines,
          dueDate: values.dueDate,
          relatedEntityType: values.relatedEntityType,
          relatedEntityId: values.relatedEntityId,
          externalReferenceId: values.externalReferenceId,
        };

        const updated = await updateRequest(id, payload);
        navigate(
          `/requests/${encodeURIComponent(updated.id)}?direction=${encodeURIComponent(returnContext.direction)}&type=${encodeURIComponent(returnContext.type)}`,
          { replace: true }
        );
      } else {
        const payload: CreateRequestPayload = {
          requestTypeId: values.requestTypeId,
          title: values.title,
          description: values.description,
          lines: values.lines,
          dueDate: values.dueDate,
          relatedEntityType: values.relatedEntityType,
          relatedEntityId: values.relatedEntityId,
          externalReferenceId: values.externalReferenceId,
        };

        const created = await createRequest(payload);
        navigate(
          `/requests/${encodeURIComponent(created.id)}?direction=${encodeURIComponent(direction)}&type=${encodeURIComponent(typeKeyFromQuery)}`,
          { replace: true }
        );
      }
    } catch (error) {
      const message =
        error instanceof Error ? error.message : t("requests.edit.error.save");
      // Не переводим страницу в "error" (иначе показывается заголовок про подготовку/загрузку формы).
      // Ошибка сохранения должна отображаться поверх уже загруженной формы.
      setSaveError(message);
    } finally {
      setSubmitting(false);
    }
  };

  if (state.kind === "loading") {
    return (
      <div
        data-testid="request-edit-loading"
        style={{
          minHeight: "40vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Spin
          tip={
            isEdit ? t("requests.edit.loading.edit") : t("requests.edit.loading.create")
          }
        />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div>
        <Alert
          data-testid="request-edit-error-alert"
          type="error"
          message={
            isEdit
              ? t("requests.edit.error.load.title")
              : t("requests.edit.error.prepare.title")
          }
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button data-testid="request-edit-back-button" onClick={handleCancel}>
          {t("common.actions.back")}
        </Button>
      </div>
    );
  }

  if (!isEdit && createContextError) {
    return (
      <div>
        <Alert
          data-testid="request-edit-create-context-error-alert"
          type="error"
          message={t("requests.edit.error.prepare.title")}
          description={createContextError}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button data-testid="request-edit-back-button" onClick={handleCancel}>
          {t("common.actions.back")}
        </Button>
      </div>
    );
  }

  if (isEdit && !request) {
    return (
      <Alert
        data-testid="request-edit-not-found-alert"
        type="error"
        message={t("requests.edit.error.notFound.title")}
        description={t("requests.edit.error.notFound.description")}
        showIcon
      />
    );
  }

  const initialValues =
    isEdit && request
      ? {
          requestTypeId: request.requestTypeId,
          title: request.title,
          description: request.description ?? "",
          lines: (request.lines ?? []).map(
            (l, idx): RequestLineInputDto => ({
              lineNo: idx + 1,
              description: l.description ?? undefined,
              quantity: l.quantity,
              needByDate: l.needByDate ?? undefined,
              supplierName: l.supplierName ?? undefined,
              supplierContact: l.supplierContact ?? undefined,
            })
          ),
          dueDate: request.dueDate ?? undefined,
          relatedEntityType: request.relatedEntityType ?? "",
          relatedEntityId: request.relatedEntityId ?? "",
          externalReferenceId: request.externalReferenceId ?? "",
        }
      : fixedCreateTypeId
        ? {
            requestTypeId: fixedCreateTypeId,
            title: "",
            description: "",
            lines: [],
            dueDate: undefined,
            relatedEntityType: "",
            relatedEntityId: "",
            externalReferenceId: "",
          }
        : undefined;

  return (
    <Card data-testid="request-edit-card">
      <Title level={3} style={{ marginBottom: 16 }}>
        {isEdit ? t("requests.edit.title.edit") : t("requests.edit.title.create")}
      </Title>

      {saveError && (
        <Alert
          data-testid="request-edit-save-error-alert"
          type="error"
          message={t("requests.edit.error.save")}
          description={saveError}
          showIcon
          style={{ marginBottom: 16 }}
        />
      )}

      <RequestForm
        mode={isEdit ? "edit" : "create"}
        requestTypeCode={request?.requestTypeCode}
        initialValues={initialValues}
        requestTypes={requestTypes}
        fixedRequestTypeId={!isEdit ? fixedCreateTypeId ?? undefined : undefined}
        submitting={submitting}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
      />
    </Card>
  );
};
